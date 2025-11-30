using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;

namespace DhanSutra.Pdf
{
    public class PurchaseInvoiceDocument : IDocument
    {
        public PurchaseInvoicePdfDto Invoice { get; }
        public CompanyProfilePdfDto Company { get; }

        public PurchaseInvoiceDocument(
            PurchaseInvoicePdfDto invoice,
            CompanyProfilePdfDto company)
        {
            Invoice = invoice;
            Company = company;
        }

        public DocumentMetadata GetMetadata() =>
            new DocumentMetadata
            {
                Title = $"Purchase Invoice {Invoice.InvoiceNo}"
            };

        public DocumentSettings GetSettings() => new DocumentSettings();

        // ─────────────────────────────────────────────────────────────
        // MAIN DOCUMENT
        // ─────────────────────────────────────────────────────────────
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);

                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeBody);
                page.Footer().AlignCenter().Text("This is a computer generated document.");
            });
        }

        // ─────────────────────────────────────────────────────────────
        // HEADER
        // ─────────────────────────────────────────────────────────────
        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                // HEADER ROW
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(Company.CompanyName).FontSize(16).Bold();
                        c.Item().Text(Company.AddressLine1);
                        c.Item().Text($"{Company.City}, {Company.State} - {Company.Pincode}");
                        c.Item().Text($"GSTIN: {Company.GSTIN}");
                        c.Item().Text($"Phone: {Company.Phone}");
                    });

                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text("Purchase Invoice").FontSize(16).Bold();
                        c.Item().Text($"Invoice No: {Invoice.InvoiceNo}");
                        c.Item().Text($"Invoice Date: {Invoice.InvoiceDate}");
                    });
                });

                // FIXED 100% — NO ERROR
                
                col.Item().PaddingVertical(10).LineHorizontal(1);
            });
        }


        // ─────────────────────────────────────────────────────────────
        // BODY
        // ─────────────────────────────────────────────────────────────
        void ComposeBody(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Element(ComposeSupplierSection);
                col.Item().PaddingTop(10).Element(ComposeTable);
                col.Item().PaddingTop(10).Element(ComposeTotals);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // SUPPLIER
        // ─────────────────────────────────────────────────────────────
        void ComposeSupplierSection(IContainer container)
        {
            container.Border(1).Padding(10).Column(col =>
            {
                col.Item().Text("Supplier Details").Bold();
                col.Item().Text($"Name: {Invoice.SupplierName}");
                col.Item().Text($"GSTIN: {Invoice.SupplierGSTIN}");
                col.Item().Text($"State: {Invoice.SupplierState}");
                col.Item().Text($"Phone: {Invoice.SupplierPhone}");
            });
        }

        // ─────────────────────────────────────────────────────────────
        // TABLE
        // ─────────────────────────────────────────────────────────────
        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                // COLUMNS
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(25);     // #
                    cols.RelativeColumn(2);      // Item
                    cols.ConstantColumn(40);     // Qty
                    cols.ConstantColumn(55);     // Rate
                    cols.ConstantColumn(55);     // Net Rate
                    cols.ConstantColumn(65);     // Net Amt
                    cols.ConstantColumn(40);     // GST%
                    cols.ConstantColumn(65);     // GST Amt
                    cols.ConstantColumn(40);     // CGST%
                    cols.ConstantColumn(55);     // CGST Amt
                    cols.ConstantColumn(40);     // SGST%
                    cols.ConstantColumn(55);     // SGST Amt
                    cols.ConstantColumn(40);     // IGST%
                    cols.ConstantColumn(55);     // IGST Amt
                    cols.ConstantColumn(65);     // Total
                });

                // HEADER ROW
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

                // NORMAL ROWS
                int index = 1;

                foreach (var item in Invoice.Items)
                {
                    table.Cell().Padding(3).BorderBottom(1).Text(index.ToString());
                    table.Cell().Padding(3).BorderBottom(1).Text(item.ItemName ?? "");
                    table.Cell().Padding(3).BorderBottom(1).Text(item.Qty.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.Rate.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.NetRate.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.LineSubTotal.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.GstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.GstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.CgstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.CgstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.SgstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.SgstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.IgstPercent.ToString("0.##"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.IgstValue.ToString("0.00"));
                    table.Cell().Padding(3).BorderBottom(1).Text(item.LineTotal.ToString("0.00"));

                    index++;
                }
            });
        }


        //void AddHeader(ITableHeaderContainer header, string text)
        //{
        //    header.Cell().Element(c =>
        //        c.Background(Colors.Grey.Lighten2)
        //         .Padding(4)
        //         .BorderBottom(1)
        //    )
        //    .Text(text).SemiBold();
        //}

        //void AddCell(ITableCellContainer cell, string text)
        //{
        //    cell.Element(c =>
        //        c.Padding(3)
        //         .BorderBottom(1)
        //    )
        //    .Text(text ?? "");
        //}

        // header cell builder
        

        // ─────────────────────────────────────────────────────────────
        // TOTALS
        // ─────────────────────────────────────────────────────────────
        void ComposeTotals(IContainer container)
        {
            container.AlignRight().Width(250).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:");
                    r.AutoItem().Text(Invoice.TotalAmount.ToString("0.00"));
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total GST:");
                    r.AutoItem().Text(Invoice.TotalTax.ToString("0.00"));
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Round Off:");
                    r.AutoItem().Text(Invoice.RoundOff.ToString("0.00"));
                });

                col.Item().PaddingTop(5).BorderTop(1).Row(r =>
                {
                    r.RelativeItem().Text("Grand Total:").Bold();
                    r.AutoItem().Text(Invoice.TotalAmount.ToString("0.00")).Bold();
                });
            });
        }
    }
}
