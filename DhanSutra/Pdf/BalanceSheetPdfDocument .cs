using DhanSutra.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DhanSutra.Pdf
{
    public class BalanceSheetPdfDocument : IDocument
    {
        private readonly BalanceSheetReportDto _report;
        private readonly DateTime _from;
        private readonly DateTime _to;

        public BalanceSheetPdfDocument(
            BalanceSheetReportDto report,
            DateTime from,
            DateTime to)
        {
            _report = report;
            _from = from;
            _to = to;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignRight()
                    .Text($"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}");
            });
        }

        // -------------------------------------------------

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("BALANCE SHEET")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // -------------------------------------------------

        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(15);

                col.Item().Element(c =>
                    ComposeSection(c, "ASSETS",
                        _report.Assets.Rows.Select(r =>
                            (r.AccountName, r.Debit)),
                        _report.Assets.Total));

                col.Item().Element(c =>
                    ComposeSection(c, "LIABILITIES",
                        _report.Liabilities.Rows.Select(r =>
                            (r.AccountName, r.Credit)),
                        _report.Liabilities.Total));

                col.Item().Element(c =>
                    ComposeSection(c, "CAPITAL",
                        _report.Capital.Rows.Select(r =>
                            (r.AccountName, r.Credit)),
                        _report.Capital.Total));
            });
        }

        // -------------------------------------------------

        void ComposeSection(
            IContainer container,
            string title,
            IEnumerable<(string Name, decimal Amount)> rows,
            decimal total)
        {
            container.Column(col =>
            {
                col.Item().Text(title).Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.ConstantColumn(100);
                    });

                    // Header
                    table.Header(h =>
                    {
                        h.Cell().Element(Cell).BorderBottom(1)
                            .Text("Account").Bold();

                        h.Cell().Element(Cell).BorderBottom(1)
                            .AlignRight().Text("Amount").Bold();
                    });

                    // Body
                    foreach (var r in rows)
                    {
                        table.Cell().Element(BodyCell)
                            .Text(r.Name);

                        table.Cell().Element(BodyCell)
                            .AlignRight()
                            .Text(r.Amount.ToString("N2"));
                    }

                    // Total
                    table.Cell().Element(BodyCell)
                        .AlignRight().Text("Total").Bold();

                    table.Cell().Element(BodyCell)
                        .AlignRight().Text(total.ToString("N2")).Bold();
                });
            });
        }

        // -------------------------------------------------
        // CELL STYLES (same as DayBook)
        // -------------------------------------------------

        static IContainer Cell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);

        static IContainer BodyCell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);
    }
}
