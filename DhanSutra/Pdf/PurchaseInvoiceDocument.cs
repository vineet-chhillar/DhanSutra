using MigraDoc.DocumentObjectModel.Tables;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DhanSutra.Pdf
{
    public class PurchaseInvoiceDocument : IDocument
    {
        public PurchaseInvoicePdfDto Invoice { get; }
        public CompanyProfilePdfDto Company { get; }

        public PurchaseInvoiceDocument(PurchaseInvoicePdfDto invoice, CompanyProfilePdfDto company)
        {
            Invoice = invoice ?? new PurchaseInvoicePdfDto();
            Company = company ?? new CompanyProfilePdfDto(default); // Pass default or null for required 'model' parameter
        }

        public DocumentMetadata GetMetadata() =>
            new DocumentMetadata { Title = $"Purchase Invoice {Invoice?.InvoiceNo ?? ""}" };

        // Minimal settings object (works with newer QuestPDF)
        public DocumentSettings GetSettings() => new DocumentSettings();

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                // header, content, footer - use Element with method group (safe)
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeBody);
                page.Footer().AlignCenter().Text("This is a computer generated document.");
            });
        }

        // ---------- Header ----------
        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    // LEFT SIDE (company info)
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(Company?.CompanyName ?? "").FontSize(16).Bold();
                        left.Item().Text(Company?.AddressLine1 ?? "");
                        left.Item().Text($"{Company?.City ?? ""}, {Company?.State ?? ""} - {Company?.Pincode ?? ""}");
                        left.Item().Text($"GSTIN: {Company?.GSTIN ?? ""}");
                        left.Item().Text($"Phone: {Company?.Phone ?? ""}");
                    });

                    // RIGHT SIDE (invoice info) - FIXED WIDTH
                    row.ConstantItem(220).Column(right =>
                    {
                        right.Item().AlignRight().Text("Purchase Invoice").FontSize(16).Bold();
                        right.Item().AlignRight().Text($"Invoice No: {Invoice?.InvoiceNo ?? ""}");
                        string invoiceDateFormatted = "";

                        if (DateTime.TryParse(Invoice?.InvoiceDate?.ToString(), out var dt))
                        {
                            invoiceDateFormatted = dt.ToString("dd-MM-yyyy");
                        }

                        right.Item().AlignRight().Text($"Invoice Date: {invoiceDateFormatted}");

                    });
                });

                col.Item().PaddingVertical(10).LineHorizontal(1);
            });
        }


        // ---------- Body ----------
        void ComposeBody(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Element(ComposeSupplierSection);
                col.Item().PaddingTop(8).Element(ComposeTable);
                col.Item().PaddingTop(8).Element(ComposeGstSummary);
                
                col.Item().PaddingTop(8).Element(ComposeTotals);
                col.Item().PaddingTop(8).Element(ComposeFooter);
            });
        }

        // ---------- Supplier ----------
        void ComposeSupplierSection(IContainer container)
        {
            container.Border(1).Padding(8).Column(col =>
            {
                col.Item().Text("Supplier Details").Bold();
                col.Item().Text($"Name: {Invoice?.SupplierName ?? ""}");
                col.Item().Text($"GSTIN: {Invoice?.SupplierGSTIN ?? ""}");
                col.Item().Text($"State: {Invoice?.SupplierState ?? ""}");
                col.Item().Text($"Phone: {Invoice?.SupplierPhone ?? ""}");
            });
        }

        // ---------- Table ----------
        void ComposeTable(IContainer container)
        {
            container
                .Border(1)
                //.BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);   // #
                        cols.RelativeColumn(3);    // Item Name
                        cols.ConstantColumn(60);   // HSN
                        cols.ConstantColumn(60);   // Batch
                        cols.ConstantColumn(45);   // Qty
                        cols.ConstantColumn(55);   // Rate
                        cols.ConstantColumn(50);   // Disc %
                        cols.ConstantColumn(70);   // Subtotal
                        cols.ConstantColumn(70);   // Total
                    });

                    // HEADER
                    table.Header(h =>
                    {

                       
                        HeaderCell(h, "#");
                        HeaderCell(h, "Item");
                        HeaderCell(h, "HSN");
                        HeaderCell(h, "Batch");
                        HeaderCell(h, "Qty");
                        HeaderCell(h, "Rate");
                        HeaderCell(h, "Disc %");
                        HeaderCell(h, "SubTotal");
                        HeaderCell(h, "Total");
                    });

                    int idx = 1;
                    foreach (var it in Invoice?.Items ?? Enumerable.Empty<PurchaseInvoiceItemPdfDto>())
                    {
                        
                        BodyCell(table, idx.ToString());
                        BodyCell(table, it.ItemName ?? "");
                        BodyCell(table, it.HsnCode.ToString());
                        BodyCell(table, it.BatchNo.ToString());
                        BodyCell(table, it.Qty.ToString("0.00"));
                        BodyCell(table, it.Rate.ToString("0.##"));
                        BodyCell(table, it.DiscountPercent.ToString("0.00"));
                        BodyCell(table, it.LineSubTotal.ToString("0.00"));
                        BodyCell(table, it.LineTotal.ToString("0.00"));

                        idx++;
                    }
                });
        }
        void ComposeGstSummary(IContainer container)
        {
            // GST summary grouped by GST %
            var summary = new Dictionary<decimal, GstSummaryRow>();

            foreach (var it in Invoice.Items)
            {
                var rate = it.GstPercent;

                if (!summary.ContainsKey(rate))
                {
                    summary[rate] = new GstSummaryRow();
                }

                summary[rate].Taxable += it.LineSubTotal;
                summary[rate].Cgst += it.CgstValue;
                summary[rate].Sgst += it.SgstValue;
                summary[rate].Igst += it.IgstValue;
            }

            if (!summary.Any())
                return;

            container
                .PaddingTop(10)
                .Border(1)
                //.BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .Column(col =>
                {
                    col.Item()
                       .Text("GST Summary")
                       .SemiBold()
                       .FontSize(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);  // GST %
                            columns.RelativeColumn(2);   // Taxable
                            columns.ConstantColumn(45);  // CGST %
                            columns.RelativeColumn(1);   // CGST Amt
                            columns.ConstantColumn(45);  // SGST %
                            columns.RelativeColumn(1);   // SGST Amt
                            columns.ConstantColumn(45);  // IGST %
                            columns.RelativeColumn(1);   // IGST Amt
                            columns.RelativeColumn(1);   // Total GST
                        });

                        // HEADER
                        table.Header(h =>
                        {
                            HeaderCell(h, "GST %");
                            HeaderCell(h, "Taxable Value");
                            HeaderCell(h, "CGST %");
                            HeaderCell(h, "CGST Amt");
                            HeaderCell(h, "SGST %");
                            HeaderCell(h, "SGST Amt");
                            HeaderCell(h, "IGST %");
                            HeaderCell(h, "IGST Amt");
                            HeaderCell(h, "Total GST");
                        });

                        // ROWS
                        foreach (var kv in summary.OrderBy(x => x.Key))
                        {
                            var rate = kv.Key;
                            var v = kv.Value;

                            var isInterState = v.Igst > 0;

                            BodyCell(table, rate.ToString("0.##"));
                            BodyCell(table, v.Taxable.ToString("0.00"));

                            BodyCell(table, isInterState ? "0.00" : (rate / 2).ToString("0.##"));
                            BodyCell(table, v.Cgst.ToString("0.00"));

                            BodyCell(table, isInterState ? "0.00" : (rate / 2).ToString("0.##"));
                            BodyCell(table, v.Sgst.ToString("0.00"));

                            BodyCell(table, isInterState ? rate.ToString("0.##") : "0.00");
                            BodyCell(table, v.Igst.ToString("0.00"));

                            BodyCell(
                                table,
                                (v.Cgst + v.Sgst + v.Igst).ToString("0.00")
                            );
                        }
                    });
                });
        }
        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("This is a computer generated invoice and does not require signature.")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });
        }

        static void HeaderCell(TableCellDescriptor header, string text)
        {
            header.Cell()
                .Padding(4)
                //.Background(Colors.Grey.Lighten2)
                .BorderBottom(1)
                .Text(text)
                .SemiBold();
        }

        static void BodyCell(TableDescriptor table, string text)
        {
            table.Cell()
                .Padding(3)
                .BorderBottom(1)
                .Text(text);
        }


        // ---------- Totals ----------
        void ComposeTotals(IContainer container)
        {
            container.AlignRight().MaxWidth(300).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:");
                    r.AutoItem().Text(Invoice?.SubTotalAmount.ToString("0.00") ?? "0.00");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total GST:");
                    r.AutoItem().Text(Invoice?.TotalTax.ToString("0.00") ?? "0.00");
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Round Off:");
                    r.AutoItem().Text(Invoice?.RoundOff.ToString("0.00") ?? "0.00");
                });

                col.Item().PaddingTop(6).BorderTop(1).Row(r =>
                {
                    r.RelativeItem().Text("Grand Total:").Bold();
                    r.AutoItem().Text(Invoice?.TotalAmount.ToString("0.00") ?? "0.00").Bold();
                });
            });
        }
        static string FormatDate(string iso)
        {
            DateTime dt;
            if (DateTime.TryParse(iso, out dt))
                return dt.ToString("dd-MMM-yyyy");
            return iso ?? "";
        }

        // Simple number to words for rupees (handles up to crores). Not fully exhaustive but sufficient.
        static string NumberToWords(long number)
        {
            if (number == 0) return "Zero Rupees";

            var units = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string words = "";

            Func<long, string> twoDigits = n =>
            {
                if (n < 20) return units[n];
                var t = n / 10;
                var u = n % 10;
                if (u == 0) return tens[t];
                return tens[t] + " " + units[u];
            };

            Func<long, string> threeDigits = n =>
            {
                var result = "";
                var h = n / 100;
                var rem = n % 100;
                if (h > 0) result += units[h] + " Hundred";
                if (rem > 0)
                {
                    if (result != "") result += " ";
                    result += twoDigits(rem);
                }
                return result;
            };

            var crore = number / 10000000;
            number %= 10000000;
            var lakh = number / 100000;
            number %= 100000;
            var thousand = number / 1000;
            number %= 1000;
            var hundred = number; // up to 999

            if (crore > 0) { words += threeDigits(crore) + " Crore"; }
            if (lakh > 0) { if (words != "") words += " "; words += threeDigits(lakh) + " Lakh"; }
            if (thousand > 0) { if (words != "") words += " "; words += threeDigits(thousand) + " Thousand"; }
            if (hundred > 0) { if (words != "") words += " "; words += threeDigits(hundred); }

            return "Rupees " + words;
        }

    }
    class GstSummaryRow
    {
        public decimal Taxable { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
    }

}
