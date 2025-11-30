using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
namespace DhanSutra.Pdf
{
    public class PurchaseInvoiceDocument : IDocument
    {
        public PurchaseInvoicePdfDto Invoice { get; }
        public CompanyProfilePdfDto Company { get; }

        public PurchaseInvoiceDocument(PurchaseInvoicePdfDto invoice, CompanyProfilePdfDto company)
        {
            Invoice = invoice;
            Company = company;
        }

        public DocumentMetadata GetMetadata() =>
            new DocumentMetadata { Title = $"Purchase Invoice {Invoice.InvoiceNo}" };

        public DocumentSettings GetSettings()
        {
            // minimal, version-safe settings object
            return new DocumentSettings();
        }


        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeBody);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("This is a computer generated document.");
                });
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Header
        // ─────────────────────────────────────────────────────────────────────────────
        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(Company.CompanyName).FontSize(16).Bold();
                    col.Item().Text(Company.AddressLine1);
                    col.Item().Text($"{Company.City}, {Company.State} - {Company.Pincode}");
                    col.Item().Text($"GSTIN: {Company.GSTIN}");
                    col.Item().Text($"Phone: {Company.Phone}");
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Purchase Invoice").FontSize(16).Bold();
                    col.Item().Text($"Invoice No: {Invoice.InvoiceNo}");
                    col.Item().Text($"Invoice Date: {Invoice.InvoiceDate}");
                });
            });

            container.PaddingVertical(10).LineHorizontal(1);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Body
        // ─────────────────────────────────────────────────────────────────────────────
        void ComposeBody(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Element(ComposeSupplierSection);
                col.Item().PaddingTop(10).Element(ComposeTable);
                col.Item().PaddingTop(10).Element(ComposeTotals);
            });
        }

        // Supplier Info
        void ComposeSupplierSection(IContainer container)
        {
            container.Border(1).Padding(10).Column(col =>
            {
                col.Item().Text("Supplier Details").Bold().FontSize(12);
                col.Item().Text($"Supplier ID: {Invoice.SupplierId}");
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Table Layout
        // ─────────────────────────────────────────────────────────────────────────────
        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(25);    // SNo
                    cols.RelativeColumn(2);     // Item Name
                    cols.ConstantColumn(70);    // Batch No
                    cols.ConstantColumn(50);    // HSN
                    cols.ConstantColumn(40);    // Qty
                    cols.ConstantColumn(55);    // Rate
                    cols.ConstantColumn(55);    // NetRate
                    cols.ConstantColumn(65);    // NetAmount
                    cols.ConstantColumn(40);    // GST%
                    cols.ConstantColumn(65);    // GST Amt
                    cols.ConstantColumn(40);    // CGST%
                    cols.ConstantColumn(55);    // CGST Amt
                    cols.ConstantColumn(40);    // SGST%
                    cols.ConstantColumn(55);    // SGST Amt
                    cols.ConstantColumn(40);    // IGST%
                    cols.ConstantColumn(55);    // IGST Amt
                    cols.ConstantColumn(65);    // Total
                });

                // ================= HEADER =================
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("#");
                    header.Cell().Element(HeaderCellStyle).Text("Item");
                    header.Cell().Element(HeaderCellStyle).Text("Batch");
                    header.Cell().Element(HeaderCellStyle).Text("HSN");
                    header.Cell().Element(HeaderCellStyle).Text("Qty");
                    header.Cell().Element(HeaderCellStyle).Text("Rate");
                    header.Cell().Element(HeaderCellStyle).Text("Net Rate");
                    header.Cell().Element(HeaderCellStyle).Text("Net Amt");
                    header.Cell().Element(HeaderCellStyle).Text("GST%");
                    header.Cell().Element(HeaderCellStyle).Text("GST Amt");
                    header.Cell().Element(HeaderCellStyle).Text("CGST%");
                    header.Cell().Element(HeaderCellStyle).Text("CGST Amt");
                    header.Cell().Element(HeaderCellStyle).Text("SGST%");
                    header.Cell().Element(HeaderCellStyle).Text("SGST Amt");
                    header.Cell().Element(HeaderCellStyle).Text("IGST%");
                    header.Cell().Element(HeaderCellStyle).Text("IGST Amt");
                    header.Cell().Element(HeaderCellStyle).Text("Total");
                });

                // ================= ROWS =================
                int index = 1;

                foreach (var item in Invoice.Items)
                {
                    table.Cell().Element(CellStyle).Text(index.ToString());

                    // ITEM NAME
                    table.Cell().Element(CellStyle).Text(item.ItemName);

                    // BATCH NO
                    table.Cell().Element(CellStyle).Text(item.BatchNo ?? "-");

                    // HSN
                    

                    table.Cell().Element(CellStyle).Text(item.Qty.ToString("0.##"));
                    table.Cell().Element(CellStyle).Text(item.Rate.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.NetRate.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.LineSubTotal.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.GstPercent.ToString("0.##"));
                    table.Cell().Element(CellStyle).Text(item.GstValue.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.CgstPercent.ToString("0.##"));
                    table.Cell().Element(CellStyle).Text(item.CgstValue.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.SgstPercent.ToString("0.##"));
                    table.Cell().Element(CellStyle).Text(item.SgstValue.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.IgstPercent.ToString("0.##"));
                    table.Cell().Element(CellStyle).Text(item.IgstValue.ToString("0.00"));
                    table.Cell().Element(CellStyle).Text(item.LineTotal.ToString("0.00"));

                    index++;
                }
            });
        }
        private IContainer HeaderCellStyle(IContainer cell)
        {
            return cell
                .Padding(3)
                .Background(Colors.Grey.Lighten2)
                .BorderBottom(1);
        }

        private IContainer CellStyle(IContainer cell)
        {
            return cell
                .Padding(2)
                .BorderBottom(1);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Totals Section
        // ─────────────────────────────────────────────────────────────────────────────
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
