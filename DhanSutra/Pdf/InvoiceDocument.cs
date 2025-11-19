using System;
using System.Collections.Generic;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DhanSutra.Pdf
{
    // DTOs (reuse your real DTOs) - kept minimal here for compilation reference
    public class InvoiceItemDto
    {
        public int ItemId { get; set; }
        public string BatchNo { get; set; }
        public string HsnCode { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }

        public decimal GstPercent { get; set; }
        public decimal GstValue { get; set; }

        public decimal CgstPercent { get; set; }
        public decimal CgstValue { get; set; }

        public decimal SgstPercent { get; set; }
        public decimal SgstValue { get; set; }

        public decimal IgstPercent { get; set; }
        public decimal IgstValue { get; set; }

        public decimal LineSubTotal { get; set; }   // before tax
        public decimal LineTotal { get; set; }      // after tax
        public string ItemName { get; set; }        // optional
    }

    public class InvoiceLoadDto
    {
        public long Id { get; set; }
        public string InvoiceNo { get; set; }
        public int InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }
        public int CompanyProfileId { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string CustomerAddress { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        public List<InvoiceItemDto> Items { get; set; }
    }

    public class CompanyProfileDto
    {
        //public string CompanyName { get; set; }
        //public string AddressLine1 { get; set; }
        //public string AddressLine2 { get; set; }
        //public string City { get; set; }
        //public string State { get; set; }
        //public string Pincode { get; set; }
        //public string GSTIN { get; set; }
        //public string PAN { get; set; }
        //public string Email { get; set; }
        //public string Phone { get; set; }


        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string Country { get; set; }

        public string GSTIN { get; set; }
        public string PAN { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string IFSC { get; set; }
        public string BranchName { get; set; }

        public string InvoicePrefix { get; set; }
        public int InvoiceStartNo { get; set; }
        public int CurrentInvoiceNo { get; set; }

        public byte[] Logo { get; set; }

        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }

    }

    // The QuestPDF document
    public class InvoiceDocument : IDocument
    {
        public readonly InvoiceLoadDto _invoice;
        public readonly CompanyProfileDto _company;

        public InvoiceDocument(InvoiceLoadDto invoice, CompanyProfileDto company)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            _company = company ?? throw new ArgumentNullException(nameof(company));
        }

        public DocumentMetadata GetMetadata() =>
            new DocumentMetadata
            {
                Title = $"Invoice {_invoice.InvoiceNo}"
            };

        public DocumentSettings GetSettings()
        {
            return DocumentSettings.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                // Left: Logo area (optional)
                row.RelativeItem(2).Column(col =>
                {
                    col.Item().Text(_company.CompanyName).FontSize(16).SemiBold();
                    col.Item().Text($"{_company.AddressLine1} {_company.AddressLine2}");
                    col.Item().Text($"{_company.City}  {_company.State} - {_company.Pincode}");
                    if (!string.IsNullOrEmpty(_company.GSTIN))
                        col.Item().Text($"GSTIN: {_company.GSTIN}");
                    if (!string.IsNullOrEmpty(_company.PAN))
                        col.Item().Text($"PAN: {_company.PAN}");
                    if (!string.IsNullOrEmpty(_company.Email) || !string.IsNullOrEmpty(_company.Phone))
                        col.Item().Text($"{_company.Email}  {_company.Phone}");
                });
#pragma warning restore CS0618 // Type or member is obsolete

                // Right: Invoice meta
                row.RelativeItem(1).AlignRight().Column(col =>
                {
                    col.Item().Text("TAX INVOICE").FontSize(14).SemiBold();
                    col.Item().PaddingTop(6).Element(c =>
                    {
                        c.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(x => x.Text($"Invoice No:").SemiBold());
                            table.Cell().Element(x => x.Text(_invoice.InvoiceNo).SemiBold());

                            table.Cell().Element(x => x.Text($"Invoice Date:"));
                            table.Cell().Element(x => x.Text(FormatDate(_invoice.InvoiceDate)));

                            table.Cell().Element(x => x.Text($"Place of Supply:"));
                            table.Cell().Element(x => x.Text(_company.State));
                        });
                    });
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingTop(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Bill To:").Bold();
                        c.Item().Text(_invoice.CustomerName ?? "");
                        c.Item().Text(_invoice.CustomerAddress ?? "");
                        c.Item().Text($"State: {_invoice.CustomerState}");
                        if (!string.IsNullOrEmpty(_invoice.CustomerPhone))
                            c.Item().Text($"Phone: {_invoice.CustomerPhone}");
                    });

                    row.ConstantItem(200).Column(c =>
                    {
                        // small right box (could include additional invoice info)
                        c.Item().Text("").FontSize(8);
                    });
                });

                col.Item().PaddingTop(10).Element(ComposeItemsTable);

                col.Item().PaddingTop(8).Element(ComposeGstSummary);

                col.Item().PaddingTop(6).Row(r =>
                {
                    r.RelativeItem()
                        .Text($"Amount (in words): {NumberToWords((long)Math.Round(_invoice.TotalAmount))} only")
                        .FontSize(10).Italic();

                    r.ConstantItem(220).AlignRight().Column(right =>
                    {
                        right.Item().Row(t =>
                        {
                            t.RelativeItem().Text("Subtotal:").SemiBold();
                            t.RelativeItem().Text(_invoice.SubTotal.ToString("N2"));
                        });

                        right.Item().Row(t =>
                        {
                            t.RelativeItem().Text("Total Tax:");
                            t.RelativeItem().Text(_invoice.TotalTax.ToString("N2"));
                        });

                        right.Item().Row(t =>
                        {
                            t.RelativeItem().Text("Round Off:");
                            t.RelativeItem().Text(_invoice.RoundOff.ToString("N2"));
                        });

                        right.Item().PaddingTop(6).Row(t =>
                        {
                            t.RelativeItem().Text("Grand Total:").Bold();
                            t.RelativeItem().Text(_invoice.TotalAmount.ToString("N2")).Bold();
                        });
                    });
                });

                col.Item().PaddingTop(16).Row(r =>
                {
                    r.RelativeItem().Text("Declaration: We declare that this invoice shows the actual price of the goods described and that all particulars are true and correct.").FontSize(9);
                    r.ConstantItem(200).Column(sign =>
                    {
                        sign.Item().Text("For " + _company.CompanyName).Bold();
                        sign.Item().PaddingTop(30).Text("Authorised Signatory").AlignCenter();
                    });
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

        void ComposeItemsTable(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(col =>
            {
                col.Item().Table(table =>
                {
                    // define columns - adjust widths as needed
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);   // S.No / item
                        columns.RelativeColumn(3);   // Item Name
                        columns.RelativeColumn(1);   // HSN
                        columns.RelativeColumn(1);   // Batch
                        columns.RelativeColumn(1);   // Qty
                        columns.RelativeColumn(1);   // Rate
                        columns.RelativeColumn(1);   // Disc %
                        columns.RelativeColumn(1);   // Subtotal
                        columns.RelativeColumn(1);   // GST %
                        columns.RelativeColumn(1);   // GST Amt
                        columns.RelativeColumn(1);   // CGST %
                        columns.RelativeColumn(1);   // CGST Amt
                        columns.RelativeColumn(1);   // SGST %
                        columns.RelativeColumn(1);   // SGST Amt
                        columns.RelativeColumn(1);   // IGST %
                        columns.RelativeColumn(1);   // IGST Amt
                        columns.RelativeColumn(1);   // Total
                    });

                    // Header row
                    table.Cell().Element(CellStyle).Text("S.No").SemiBold();
                    table.Cell().Element(CellStyle).Text("Item & Description").SemiBold();
                    table.Cell().Element(CellStyle).Text("HSN").SemiBold();
                    table.Cell().Element(CellStyle).Text("Batch").SemiBold();
                    table.Cell().Element(CellStyle).Text("Qty").SemiBold();
                    table.Cell().Element(CellStyle).Text("Rate").SemiBold();
                    table.Cell().Element(CellStyle).Text("Disc %").SemiBold();
                    table.Cell().Element(CellStyle).Text("SubTotal").SemiBold();
                    table.Cell().Element(CellStyle).Text("GST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("GST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("CGST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("CGST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("SGST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("SGST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("IGST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("IGST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("Total").SemiBold();

                    // Rows
                    var items = _invoice.Items ?? new List<InvoiceItemDto>();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = items[i];
                        table.Cell().Element(CellStyle).Text((i + 1).ToString());
                        table.Cell().Element(CellStyle).Text(it.ItemName ?? "");
                        table.Cell().Element(CellStyle).Text(it.HsnCode ?? "");
                        table.Cell().Element(CellStyle).Text(it.BatchNo ?? "");
                        table.Cell().Element(CellStyle).Text(it.Qty.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.Rate.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.DiscountPercent.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.LineSubTotal.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.GstPercent.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.GstValue.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.CgstPercent.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.CgstValue.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.SgstPercent.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.SgstValue.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.IgstPercent.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.IgstValue.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(it.LineTotal.ToString("N2"));
                    }
                });
            });
        }

        void ComposeGstSummary(IContainer container)
        {
            // Summarize GST by rate (simple approach: total across items)
            var summary = new Dictionary<decimal, (decimal taxable, decimal cgst, decimal sgst, decimal igst)>();

            foreach (var it in _invoice.Items ?? new List<InvoiceItemDto>())
{
                decimal rate = it.GstPercent;
                if (!summary.ContainsKey(rate))
                    summary[rate] = (0m, 0m, 0m, 0m);

                var tuple = summary[rate];
                tuple.taxable += it.LineSubTotal;
                tuple.cgst += it.CgstValue;
                tuple.sgst += it.SgstValue;
                tuple.igst += it.IgstValue;
                summary[rate] = tuple;
            }

            container.Padding(6).Border(1).BorderColor(Colors.Grey.Lighten3).Column(col =>
            {
                col.Item().Text("GST Summary").SemiBold();

                col.Item().Table(table =>
                {
                    // columns: rate | taxable | cgst% | cgstval | sgst% | sgstval | igst% | igstval | total
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // header
                    table.Cell().Element(CellStyle).Text("GST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("Taxable Value").SemiBold();
                    table.Cell().Element(CellStyle).Text("CGST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("CGST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("SGST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("SGST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("IGST %").SemiBold();
                    table.Cell().Element(CellStyle).Text("IGST Amt").SemiBold();
                    table.Cell().Element(CellStyle).Text("Total GST").SemiBold();

                    foreach (var kv in summary)
                    {
                        var rate = kv.Key;
                        var vals = kv.Value;

                        table.Cell().Element(CellStyle).Text(rate.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(vals.taxable.ToString("N2"));
                        table.Cell().Element(CellStyle).Text((rate / 2).ToString("N2"));
                        table.Cell().Element(CellStyle).Text(vals.cgst.ToString("N2"));
                        table.Cell().Element(CellStyle).Text((rate / 2).ToString("N2"));
                        table.Cell().Element(CellStyle).Text(vals.sgst.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(rate.ToString("N2"));
                        table.Cell().Element(CellStyle).Text(vals.igst.ToString("N2"));
                        table.Cell().Element(CellStyle).Text((vals.cgst + vals.sgst + vals.igst).ToString("N2"));
                    }
                });
            });
        }

        // small cell style helper
        IContainer CellStyle(IContainer c)
        {
            return c.Padding(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
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
}
