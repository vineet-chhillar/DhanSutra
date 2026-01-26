using DhanSutra.Models;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace DhanSutra.Pdf
{
       public class LedgerPdfDocument : IDocument
    {
        private readonly LedgerReportDto _report;

        public LedgerPdfDocument(LedgerReportDto report)
        {
            _report = report;
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

        // -------------------------------------------------

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("ACCOUNT STATEMENT")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"{_report.AccountName}")
                    .FontSize(11)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"From {_report.From:dd-MM-yyyy} To {_report.To:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // -------------------------------------------------

        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(10);

                col.Item().Text(
                    $"Opening Balance : {_report.OpeningBalance:N2} {_report.OpeningSide}"
                ).Bold();

                col.Item().Element(ComposeTable);

                col.Item().PaddingTop(5).LineHorizontal(1);

                col.Item().AlignRight().Text(
                    $"Closing Balance : {_report.ClosingBalance:N2} {_report.ClosingSide}"
                ).Bold();
            });
        }

        // -------------------------------------------------

        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);   // S.No
                    columns.ConstantColumn(65);   // Date
                    columns.RelativeColumn(2);    // Narration
                    columns.ConstantColumn(90);   // Voucher
                    columns.ConstantColumn(65);   // Debit
                    columns.ConstantColumn(65);   // Credit
                    columns.ConstantColumn(75);   // Balance
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("S.No").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Date").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Narration").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Voucher").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Credit").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance").Bold();
                });


                // ---------- BODY ----------
                int serial = 1;

                foreach (var r in _report.Rows)
                {
                    table.Cell().Element(BodyCell).Text(serial++.ToString());

                    table.Cell().Element(BodyCell)
                        .Text(r.Date.ToString("dd-MM-yyyy"));


                    table.Cell().Element(BodyCell)
                        .Text(r.Narration ?? "");

                    table.Cell().Element(BodyCell)
                        .Text($"{r.VoucherType} #{r.VoucherId}");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(r.Debit > 0 ? r.Debit.ToString("N2") : "");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(r.Credit > 0 ? r.Credit.ToString("N2") : "");

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text($"{r.RunningBalance:N2} {r.RunningSide}");
                }
            });
        }

        // -------------------------------------------------
        // CELL STYLES
        // -------------------------------------------------
        static IContainer HeaderCellStyle(IContainer c) =>
    c.BorderLeft(0.5f)
     .BorderRight(0.5f)
     .BorderBottom(1)
     .PaddingHorizontal(4)
     .PaddingVertical(3);

        static void HeaderCell(TableDescriptor table, string text, bool right = false)
        {
            var cell = table.Cell().Element(c =>
                c.BorderLeft(0.5f)
                 .BorderRight(0.5f)
                 .BorderBottom(1)
                 .PaddingHorizontal(4)
                 .PaddingVertical(3)
            );

            if (right) cell.AlignRight();
            cell.Text(text).Bold();
        }

        static IContainer BodyCell(IContainer c) =>
            c.BorderLeft(0.5f)
             .BorderRight(0.5f)
             .BorderBottom(0.5f)
             .PaddingHorizontal(4)
             .PaddingVertical(2);
    }

}
