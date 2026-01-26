using DhanSutra.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DhanSutra.Pdf
{
   

    public class DayBookPdfDocument : IDocument
    {
        private readonly List<DayBookRowDto> _rows;
        private readonly DateTime _from;
        private readonly DateTime _to;

        public DayBookPdfDocument(
            List<DayBookRowDto> rows,
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
                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Generated on: ");
                    x.Span(DateTime.Now.ToString("dd-MM-yyyy HH:mm"));
                });
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter().Text("DAY BOOK")
                    .FontSize(14).Bold();

                col.Item().AlignCenter().Text(
                    $"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}")
                    .FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        void ComposeTable(IContainer container)
        {
            decimal totalDebit = 0;
            decimal totalCredit = 0;

            int serialNo = 1;   // Group-wise S.No
            

            var groups = _rows
                .GroupBy(x => new
                {
                    x.VoucherType,
                    x.VoucherId
                });

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);    // S.No
                    columns.ConstantColumn(60);    // Date
                    columns.ConstantColumn(100);   // Voucher Type
                    columns.RelativeColumn(2);     // Description (wider)
                    columns.RelativeColumn(1);     // Account (narrower)
                    columns.ConstantColumn(65);    // Debit
                    columns.ConstantColumn(65);    // Credit
                });


                /* 🔹 TABLE HEADER */
                table.Header(header =>
                {
                    header.Cell().Element(Cell).BorderBottom(1).Text("S.No").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).Text("Date").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).Text("Voucher Type").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).Text("Description").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).Text("Account").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(Cell).BorderBottom(1).AlignRight().Text("Credit").Bold();
                });

                //table.Cell().ColumnSpan(7)
                //       .PaddingTop(8)
                //       .LineHorizontal(1);

                foreach (var grp in groups)
                {
                    bool isFirstLine = true;
                    int currentSerial = serialNo++;

                    int rowIndex = 0;
                    int rowCount = grp.Count();

                    decimal voucherDebit = 0;
                    decimal voucherCredit = 0;

                    foreach (var r in grp)
                    {
                        voucherDebit += r.Debit;
                        voucherCredit += r.Credit;

                        bool isLastLine = rowIndex == rowCount - 1;

                        // 🔹 S.No (group column)
                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? currentSerial.ToString() : "");

                        // 🔹 Date
                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? r.EntryDate.ToString("dd-MM-yyyy") : "");

                        // 🔹 Voucher Type
                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? $"{grp.Key.VoucherType} / {r.VoucherId}" : "");

                        // 🔹 Description
                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? (r.Description ?? "") : "");

                        // 🔹 Account (line-wise)
                        table.Cell().Element(LineCell)
                            .Text(r.AccountName);

                        // 🔹 Debit (line-wise)
                        table.Cell().Element(LineCell).AlignRight()
                            .Text(r.Debit == 0 ? "" : r.Debit.ToString("N2"));

                        // 🔹 Credit (line-wise)
                        table.Cell().Element(LineCell).AlignRight()
                            .Text(r.Credit == 0 ? "" : r.Credit.ToString("N2"));

                        totalDebit += r.Debit;
                        totalCredit += r.Credit;

                        isFirstLine = false;
                        rowIndex++;
                       
                    }
                    // 🔹 Voucher-wise Total Row
                    table.Cell().ColumnSpan(5)
                        .Element(BodyCell)
                        .AlignRight()
                        .Text("Voucher Total :")
                        .Bold();

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(voucherDebit.ToString("N2"))
                        .Bold();

                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(voucherCredit.ToString("N2"))
                        .Bold();

                }



                /* 🔹 GRAND TOTAL */
                table.Cell().ColumnSpan(5)
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
        
        static IContainer Cell(IContainer container)
        {
            return container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .PaddingHorizontal(4)
                .PaddingVertical(2);
        }

        static IContainer CellWithBottomLine(IContainer container)
        {
            return container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .BorderBottom(0.5f)   // 👈 horizontal line
                .PaddingHorizontal(4)
                .PaddingVertical(2);
        }
        static IContainer HeaderCell(IContainer container)
        {
            return container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .BorderBottom(1)
                .PaddingHorizontal(4)
                .PaddingVertical(3);
        }

        static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .BorderBottom(0.5f)   // 👈 horizontal line for every row
                .PaddingHorizontal(4)
                .PaddingVertical(2);
        }
        static IContainer GroupCell(IContainer container, bool drawBottom)
        {
            var c = container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .PaddingHorizontal(4)
                .PaddingVertical(2);

            if (drawBottom)
                c = c.BorderBottom(0.5f);

            return c;
        }

        static IContainer LineCell(IContainer container)
        {
            return container
                .BorderLeft(0.5f)
                .BorderRight(0.5f)
                .BorderBottom(0.5f)   // always draw line
                .PaddingHorizontal(4)
                .PaddingVertical(2);
        }



    }

}
