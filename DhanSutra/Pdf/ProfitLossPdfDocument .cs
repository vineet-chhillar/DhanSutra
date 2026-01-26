using DhanSutra.Models;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
namespace DhanSutra.Pdf
{
    public class ProfitLossPdfDocument : IDocument
    {
        private readonly ProfitLossReportDto _report;
        private readonly DateTime _from;
        private readonly DateTime _to;

        public ProfitLossPdfDocument(
            ProfitLossReportDto report,
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
                page.Footer().AlignRight().Text(
                    $"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}"
                );
            });
        }

        // --------------------------------------------------

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("PROFIT & LOSS STATEMENT")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // --------------------------------------------------

        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(15);

                col.Item().Text("Income").FontSize(11).Bold();
                col.Item().Element(c => ComposeTable(c, _report.Income));

                col.Item().Text("Expenses").FontSize(11).Bold();
                col.Item().Element(c => ComposeTable(c, _report.Expenses));

                col.Item().PaddingTop(10).LineHorizontal(1);

                col.Item().AlignCenter().Text(text =>
                {
                    if (_report.NetProfit > 0)
                        text.Span($"Net Profit : {_report.NetProfit:N2}").Bold().FontSize(12);
                    else
                        text.Span($"Net Loss : {_report.NetLoss:N2}").Bold().FontSize(12);
                });
            });
        }

        // --------------------------------------------------

        void ComposeTable(IContainer container, List<ProfitLossRow> rows)
        {
            decimal totalDebit = 0;
            decimal totalCredit = 0;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);  // Account
                    columns.ConstantColumn(90); // Debit
                    columns.ConstantColumn(90); // Credit
                });

                // ---------- HEADER ----------
                table.Header(header =>
                {
                    header.Cell().Element(Cell).BorderBottom(1).Text("Account").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).AlignRight().Text("Credit").Bold();
                });

                // ---------- BODY ----------
                foreach (var r in rows)
                {
                    table.Cell().Element(BodyCell).Text(r.AccountName);

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(r.Debit > 0 ? r.Debit.ToString("N2") : "");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(r.Credit > 0 ? r.Credit.ToString("N2") : "");

                    totalDebit += r.Debit;
                    totalCredit += r.Credit;
                }

                // ---------- TOTAL ----------
                table.Cell().Element(BodyCell).AlignRight().Text("TOTAL").Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totalDebit.ToString("N2")).Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totalCredit.ToString("N2")).Bold();
            });
        }

        // --------------------------------------------------
        // CELL STYLES (same as DayBook / TrialBalance)
        // --------------------------------------------------

        static IContainer Cell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);

        static IContainer BodyCell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);
    }

}
