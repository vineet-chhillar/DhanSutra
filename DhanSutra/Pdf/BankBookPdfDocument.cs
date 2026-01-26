using DhanSutra.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DhanSutra.Pdf
{
    using MigraDoc.DocumentObjectModel.Tables;
    using QuestPDF.Fluent;
    using QuestPDF.Helpers;
    using QuestPDF.Infrastructure;
    using System;
    using System.Linq;

    public class BankBookPdfDocument : IDocument
    {
        private readonly CashBookDto _dto;
        private readonly DateTime _from;
        private readonly DateTime _to;

        public BankBookPdfDocument(CashBookDto dto, DateTime from, DateTime to)
        {
            _dto = dto;
            _from = from;
            _to = to;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // --------------------------------------------------
        // COMPOSE
        // --------------------------------------------------
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().ShowOnce().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Generated on: ");
                    x.Span(DateTime.Now.ToString("dd-MM-yyyy HH:mm"));
                });
            });
        }

        // --------------------------------------------------
        // HEADER
        // --------------------------------------------------
        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter()
                    .Text("BANK BOOK")
                    .FontSize(14)
                    .Bold();

                col.Item().AlignCenter()
                    .Text($"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        // --------------------------------------------------
        // CONTENT
        // --------------------------------------------------
        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(10);

                col.Item().Text($"Opening Balance : {_dto.OpeningBalance:N2}")
                    .Bold();

                col.Item().Element(ComposeTable);

                col.Item().PaddingTop(5).LineHorizontal(1);

                col.Item().AlignRight()
                    .Text($"Closing Balance : {_dto.ClosingBalance:N2}")
                    .Bold();
            });
        }

        // --------------------------------------------------
        // TABLE
        // --------------------------------------------------
        void ComposeTable(IContainer container)
        {
            int serialNo = 1;

            var groups = _dto.Rows
                .GroupBy(x => new { x.VoucherType, x.VoucherId });

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(100);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(70);
                });

                // ---------- HEADER ----------
                table.Header(header =>
                {
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).Text("S.No").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).Text("Date").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).Text("Voucher Type").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).Text("Description").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).Text("Account").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).AlignRight().Text("Credit").Bold();
                    header.Cell().Element(c => Cell(c)).BorderBottom(1).AlignRight().Text("Balance").Bold();
                });

                // ---------- FOOTER ----------
                table.Footer(footer =>
                {
                    footer.Cell().ColumnSpan(5)
                        .Element(c => BodyCellStyle(c))
                        .AlignRight()
                        .Text("TOTAL")
                        .Bold();

                    footer.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                        .Text(_dto.TotalDebit.ToString("N2"))
                        .Bold();

                    footer.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                        .Text(_dto.TotalCredit.ToString("N2"))
                        .Bold();

                    footer.Cell().Element(c => BodyCellStyle(c)).Text("");
                });

                // ---------- BODY ----------
                foreach (var grp in groups)
                {
                    bool first = true;
                    int rowIndex = 0;
                    int rowCount = grp.Count();
                    int currentSerial = serialNo++;

                    decimal voucherDebit = 0;
                    decimal voucherCredit = 0;

                    foreach (var r in grp)
                    {
                        bool isLast = rowIndex == rowCount - 1;

                        voucherDebit += r.Debit;
                        voucherCredit += r.Credit;

                        table.Cell().Element(c => GroupCellStyle(c, isLast))
                            .Text(first ? currentSerial.ToString() : "");

                        table.Cell().Element(c => GroupCellStyle(c, isLast))
                            .Text(first ? r.Date.ToString("dd-MM-yyyy") : "");

                        table.Cell().Element(c => GroupCellStyle(c, isLast))
                            .Text(first ? $"{grp.Key.VoucherType} / {r.VoucherId}" : "");

                        table.Cell().Element(c => GroupCellStyle(c, isLast))
                            .Text(first ? (r.Description ?? "") : "");

                        table.Cell().Element(c => BodyCellStyle(c)).Text(r.AccountName);

                        table.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                            .Text(r.Debit == 0 ? "" : r.Debit.ToString("N2"));

                        table.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                            .Text(r.Credit == 0 ? "" : r.Credit.ToString("N2"));

                        table.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                            .Text(r.RunningBalance?.ToString("N2") ?? "");

                        first = false;
                        rowIndex++;
                    }

                    // Voucher total
                    table.Cell().ColumnSpan(5)
                        .Element(c => BodyCellStyle(c))
                        .AlignRight()
                        .Text("Voucher Total :")
                        .Bold();

                    table.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                        .Text(voucherDebit.ToString("N2"))
                        .Bold();

                    table.Cell().Element(c => BodyCellStyle(c)).AlignRight()
                        .Text(voucherCredit.ToString("N2"))
                        .Bold();

                    table.Cell().Element(c => BodyCellStyle(c)).Text("");
                }
            });
        }




        // --------------------------------------------------
        // CELL HELPERS
        // --------------------------------------------------
        static void HeaderCell(TableDescriptor t, string text, bool right = false)
        {
            var cell = t.Cell().Element(HeaderCellStyle);
            if (right) cell.AlignRight();
            cell.Text(text).Bold();
        }

        static void GroupCell(TableDescriptor t, bool bottom, string text)
        {
            t.Cell()
             .Element(c => GroupCellStyle(c, bottom))
             .Text(text);
        }

        static void LineCell(TableDescriptor t, string text, bool right = false)
        {
            var cell = t.Cell().Element(BodyCellStyle);
            if (right) cell.AlignRight();
            cell.Text(text);
        }
        static IContainer Cell(IContainer container)
        {
            return container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .PaddingHorizontal(4)
                .PaddingVertical(2);
        }
        // --------------------------------------------------
        // STYLES
        // --------------------------------------------------
        static IContainer HeaderCellStyle(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(1)
             .PaddingHorizontal(4).PaddingVertical(3);

        static IContainer BodyCellStyle(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);

        static IContainer GroupCellStyle(IContainer c, bool bottom)
        {
            c = c.BorderLeft(0.5f).BorderRight(0.5f)
                 .PaddingHorizontal(4).PaddingVertical(2);
            return bottom ? c.BorderBottom(0.5f) : c;
        }
    }


}
