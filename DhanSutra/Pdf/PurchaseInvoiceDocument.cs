using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

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
                col.Item().PaddingTop(8).Element(ComposeTotals);
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
            container.Table(table =>
            {
                // columns
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(25);     // #
                    cols.RelativeColumn(2);      // item
                    cols.ConstantColumn(25);     // qty
                    cols.ConstantColumn(25);     // rate
                    cols.ConstantColumn(25);     // net rate
                    cols.ConstantColumn(25);     // net amt
                    cols.ConstantColumn(25);     // gst%
                    cols.ConstantColumn(25);     // gst amt
                    cols.ConstantColumn(25);     // cgst%
                    cols.ConstantColumn(25);     // cgst amt
                    cols.ConstantColumn(25);     // sgst%
                    cols.ConstantColumn(25);     // sgst amt
                    cols.ConstantColumn(25);     // igst%
                    cols.ConstantColumn(25);     // igst amt
                    cols.ConstantColumn(25);     // total
                });

                // header row (each header cell is a single fluent chain)
                table.Header(header =>
                {
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("#").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("Item").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("Qty").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("Rate").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("Net Rate").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("Net Amt").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("GST%").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("GST Amt").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("CGST%").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("CGST Amt").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("SGST%").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("SGST Amt").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("IGST%").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("IGST Amt").SemiBold();
                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).Text("Total").SemiBold();
                });

                // rows
                int idx = 1;
                foreach (var it in Invoice?.Items ?? Enumerable.Empty<PurchaseInvoiceItemPdfDto>())
                {
                    table.Cell().Padding(3).BorderBottom(1).Text(idx.ToString());
                    //table.Cell().Padding(3).BorderBottom(1).Text($"{it.ItemName ?? ""}\n{it.HsnCode ?? ""}");
                    table.Cell().Padding(3).BorderBottom(1).Text(it.ItemName);
                    table.Cell().Padding(3).BorderBottom(1).Text(it.Qty.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.Rate.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.NetRate.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.LineSubTotal.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.GstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.GstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.CgstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.CgstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.SgstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.SgstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.IgstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.IgstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(it.LineTotal.ToString("0.00"));

                    idx++;
                }
            });
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

    }
}
