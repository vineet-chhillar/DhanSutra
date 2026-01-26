using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace DhanSutra.Pdf
{
    
    public class StockValuationPdfDocument : IDocument
    {
        private readonly List<StockValuationRow> _rows;
        private readonly DateTime _from;
        private readonly DateTime _to;
        decimal totOpeningQty = 0;
        decimal totOpeningVal = 0;
        decimal totInQty = 0;
        decimal totInVal = 0;
        decimal totOutQty = 0;
        decimal totOutVal = 0;
        decimal totClosingQty = 0;
        decimal totClosingVal = 0;
        public StockValuationPdfDocument(
            List<StockValuationRow> rows,
            DateTime from,
            DateTime to)
        {
            _rows = rows;
            _from = from;
            _to = to;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            

            foreach (var r in _rows)
            {
                totOpeningQty += r.OpeningQty;
                totOpeningVal += r.OpeningValue;
                totInQty += r.InQty;
                totInVal += r.InValue;
                totOutQty += r.OutQty;
                totOutVal += r.OutValue;
                totClosingQty += r.ClosingQty;
                totClosingVal += r.ClosingValue;
            }

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeTable);
                page.Footer().AlignRight()
                    .Text($"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}");
            });
        }

        // --------------------------------------------------

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("STOCK VALUATION (FIFO)")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // --------------------------------------------------

        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // S.No
                    columns.RelativeColumn(3);    // Item
                    columns.ConstantColumn(45);   // Op Qty
                    columns.ConstantColumn(55);   // Op Value
                    columns.ConstantColumn(45);   // In Qty
                    columns.ConstantColumn(55);   // In Value
                    columns.ConstantColumn(45);   // Out Qty
                    columns.ConstantColumn(55);   // COGS
                    columns.ConstantColumn(45);   // Cl Qty
                    columns.ConstantColumn(60);   // Cl Value
                });


                // ---------- HEADER ----------
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("S.No").Bold();
                    header.Cell().Element(HeaderCell).Text("Item").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Op Qty").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Op Value").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("In Qty").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("In Value").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Out Qty").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("COGS").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cl Qty").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cl Value").Bold();
                });

                // ---------- BODY ----------
                int i = 1;
                foreach (var r in _rows)
                {
                    table.Cell().Element(BodyCell).Text(i++.ToString());
                    table.Cell().Element(BodyCell).Text(r.ItemName);

                    table.Cell().Element(BodyCell).AlignRight().Text(r.OpeningQty.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.OpeningValue.ToString("N2"));

                    table.Cell().Element(BodyCell).AlignRight().Text(r.InQty.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.InValue.ToString("N2"));

                    table.Cell().Element(BodyCell).AlignRight().Text(r.OutQty.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.OutValue.ToString("N2"));

                    table.Cell().Element(BodyCell).AlignRight().Text(r.ClosingQty.ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text(r.ClosingValue.ToString("N2"));
                }
                // ---------------- TOTAL ROW ----------------
                table.Cell().Element(BodyCell).Text(""); // S.No
                table.Cell().Element(BodyCell).Text("TOTAL").Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totOpeningQty.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totOpeningVal.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totInQty.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totInVal.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totOutQty.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totOutVal.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totClosingQty.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totClosingVal.ToString("N2")).Bold();

            });
        }

        // --------------------------------------------------
        static IContainer TotalCell(IContainer c) =>
    c.Background(Colors.Grey.Lighten3)
     .Border(0.5f)
     .PaddingHorizontal(4)
     .PaddingVertical(3);

        static IContainer HeaderCell(IContainer c) =>
            c.BorderBottom(1)
             .PaddingVertical(4)
             .PaddingHorizontal(3);

        static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f)
             .PaddingVertical(3)
             .PaddingHorizontal(3);
    }

}
