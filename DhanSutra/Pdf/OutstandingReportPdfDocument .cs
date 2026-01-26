using DhanSutra.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DhanSutra.Pdf
{
    public class OutstandingReportPdfDocument : IDocument
    {
        private readonly List<OutstandingRowDto> _rows;
        private readonly string _balanceType;

        public OutstandingReportPdfDocument(
            List<OutstandingRowDto> rows,
            string balanceType)
        {
            _rows = rows;
            _balanceType = balanceType;
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
                page.Footer().AlignRight()
                    .Text($"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}");
            });
        }

        // ----------------------------------------------------

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("OUTSTANDING REPORT")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"Balance Type: {_balanceType}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // ----------------------------------------------------

        void ComposeTable(IContainer container)
        {
            decimal totalDebit = 0;
            decimal totalCredit = 0;
            decimal totalBalance = 0;
            int serial = 1;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);     // S.No
                    columns.RelativeColumn(3);      // Account
                    columns.ConstantColumn(80);     // Debit
                    columns.ConstantColumn(80);     // Credit
                    columns.ConstantColumn(90);     // Balance
                });

                // ---------- HEADER ----------
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("S.No").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Account").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Credit").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance").Bold();
                });


                // ---------- BODY ----------
                foreach (var r in _rows)
                {
                    table.Cell().Element(BodyCell).Text(serial++.ToString());
                    table.Cell().Element(BodyCell).Text(r.AccountName);

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(r.TotalDebit > 0 ? r.TotalDebit.ToString("N2") : "");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(r.TotalCredit > 0 ? r.TotalCredit.ToString("N2") : "");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(Math.Abs(r.Balance).ToString("N2"));

                    totalDebit += r.TotalDebit;
                    totalCredit += r.TotalCredit;
                    totalBalance += r.Balance;
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

                table.Cell().Element(BodyCell).AlignRight()
                    .Text(Math.Abs(totalBalance).ToString("N2"))
                    .Bold();
            });
        }

        // ----------------------------------------------------
        // CELL HELPERS (same style everywhere)
        // ----------------------------------------------------
        static IContainer HeaderCellStyle(IContainer c) =>
    c.BorderLeft(0.5f)
     .BorderRight(0.5f)
     .BorderBottom(1)
     .Padding(4);

        static void HeaderCell(TableDescriptor t, string text, bool right = false)
        {
            var c = t.Cell().Element(cn =>
                cn.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(1)
                  .Padding(4));

            if (right) c.AlignRight();
            c.Text(text).Bold();
        }

        static IContainer BodyCell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
             .Padding(4);
    }

}
