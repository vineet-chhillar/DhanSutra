using DhanSutra.Models;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace DhanSutra.Pdf
{


    public class StockSummaryPdfDocument : IDocument
    {
        private readonly List<StockValuationService.StockSummaryRow> _rows;
        private readonly DateTime _asOf;

        public StockSummaryPdfDocument(
            List<StockValuationService.StockSummaryRow> rows,
            DateTime asOf)
        {
            _rows = rows;
            _asOf = asOf;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeTable);

                page.Footer().AlignRight().Text(
                    $"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}"
                );
            });
        }

        // ----------------------------------------------------

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("STOCK SUMMARY")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"As on {_asOf:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // ----------------------------------------------------

        void ComposeTable(IContainer container)
        {
            int serialNo = 1;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // S.No
                    columns.RelativeColumn(3);    // Item
                    columns.RelativeColumn(1);    // Qty
                    columns.RelativeColumn(1.5f); // FIFO Value
                    columns.RelativeColumn(1.5f); // Avg Cost
                    columns.RelativeColumn(1.5f); // Last Purchase
                    columns.RelativeColumn(1.5f); // Selling Price
                    columns.RelativeColumn(1);    // Margin %
                    columns.RelativeColumn(1);    // Reorder
                    columns.RelativeColumn(1);    // Status
                });


                // ---------- HEADER ----------
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("S.No").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Item").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("FIFO Value").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Avg Cost").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Last Purchase").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Selling Price").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Margin %").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Reorder").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Status").Bold();
                });


                // ---------- BODY ----------
                foreach (var r in _rows)
                {
                    bool lowStock = r.Status == "Low Stock";

                    table.Cell().Element(BodyCell).Text(serialNo++.ToString());
                    table.Cell().Element(BodyCell).Text(r.ItemName);
                    table.Cell().Element(BodyCell).AlignRight().Text(r.Qty.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.FifoValue.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.AvgCost.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.LastPurchasePrice.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.SellingPrice.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.MarginPercent.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.ReorderLevel.ToString("N2"));

                    table.Cell().Element(c =>
                        BodyCell(c)
                            .Background(lowStock ? Colors.Red.Lighten4 : Colors.White)
                    ).AlignCenter().Text(r.Status);
                }
            });
        }

        // ----------------------------------------------------
        // CELL HELPERS (SAFE — no descriptor mismatch)
        // ----------------------------------------------------

        static IContainer HeaderCellStyle(IContainer c) =>
     c.BorderLeft(0.5f)
      .BorderRight(0.5f)
      .BorderBottom(1)
      .PaddingHorizontal(4)
      .PaddingVertical(3);



        static IContainer BodyCell(IContainer c) =>
    c.BorderLeft(0.5f)
     .BorderRight(0.5f)
     .BorderBottom(0.5f)
     .PaddingHorizontal(4)
     .PaddingVertical(2);

    }


}
