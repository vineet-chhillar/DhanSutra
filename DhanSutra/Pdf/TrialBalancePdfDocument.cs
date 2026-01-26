using DhanSutra.Models;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;


namespace DhanSutra.Pdf
{
    public class TrialBalancePdfDocument : IDocument
    {
        private readonly List<TrialBalanceRowDto> _rows;
        private readonly DateTime _from;
        private readonly DateTime _to;

        public TrialBalancePdfDocument(
            List<TrialBalanceRowDto> rows,
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
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

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
                    .Text("TRIAL BALANCE")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // ----------------------------------------------------

        void ComposeTable(IContainer container)
        {
            decimal totalDebit = 0;
            decimal totalCredit = 0;
            int serialNo = 1;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);   // S.No
                    columns.RelativeColumn(3);    // Account
                    columns.ConstantColumn(80);   // Debit
                    columns.ConstantColumn(80);   // Credit
                    
                });

                // ---------- HEADER ----------
                table.Header(header =>
                {
                    header.Cell().Element(Cell).BorderBottom(1).Text("S.No").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).Text("Account Name").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).AlignRight().Text("Credit").Bold();
                    
                });

                // ---------- BODY ----------
                foreach (var r in _rows)
                {
                    decimal dr = r.ClosingSide == "Dr" ? r.ClosingBalance : 0;
                    decimal cr = r.ClosingSide == "Cr" ? r.ClosingBalance : 0;

                    table.Cell().Element(BodyCell).Text(serialNo++.ToString());
                    table.Cell().Element(BodyCell).Text(r.AccountName);

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(dr > 0 ? dr.ToString("N2") : "");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(cr > 0 ? cr.ToString("N2") : "");

                    totalDebit += dr;
                    totalCredit += cr;
                }



                // ---------- TOTAL ----------
                table.Cell().ColumnSpan(2)
                    .Element(BodyCell)
                    .AlignRight()
                    .Text("TOTAL")
                    .Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totalDebit.ToString("N2"))
                    .Bold();

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(totalCredit.ToString("N2"))
                    .Bold();
            });
        }

        // ----------------------------------------------------
        // CELL STYLES (same as DayBook)
        // ----------------------------------------------------

        static IContainer Cell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);

        static IContainer BodyCell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);
    }

}
