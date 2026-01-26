using DhanSutra.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DhanSutra.Pdf
{
    public class VoucherReportPdfDocument : IDocument
    {
        private readonly List<VoucherReportRowDto> _rows;
        private readonly DateTime _from;
        private readonly DateTime _to;
        private readonly string _voucherType;

        public VoucherReportPdfDocument(
            List<VoucherReportRowDto> rows,
            DateTime from,
            DateTime to,
            string voucherType
        )
        {
            _rows = rows;
            _from = from;
            _to = to;
            _voucherType = voucherType;
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

                // 🔹 Header only on first page
                page.Header().ShowOnce().Element(ComposeHeader);

                page.Content().Element(ComposeTable);

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Generated on: ");
                    x.Span(DateTime.Now.ToString("dd-MM-yyyy HH:mm"));
                });
            });
        }
        void ComposeTable(IContainer container)
        {
            decimal totalDebit = 0;
            decimal totalCredit = 0;
            int serialNo = 1;

            var groups = _rows
                .GroupBy(x => new { x.VoucherType, x.VoucherId });

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);   // S.No
                    columns.ConstantColumn(70);   // Date
                    columns.ConstantColumn(110);  // Voucher
                    columns.RelativeColumn(2);    // Description
                    columns.RelativeColumn(1);    // Account
                    columns.ConstantColumn(60);   // Debit
                    columns.ConstantColumn(60);   // Credit
                });

                // 🔹 Table header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("S.No").Bold();
                    header.Cell().Element(HeaderCell).Text("Date").Bold();
                    header.Cell().Element(HeaderCell).Text("Voucher").Bold();
                    header.Cell().Element(HeaderCell).Text("Description").Bold();
                    header.Cell().Element(HeaderCell).Text("Account").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Debit").Bold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Credit").Bold();
                });

                foreach (var grp in groups)
                {
                    decimal voucherDebit = 0;
                    decimal voucherCredit = 0;

                    bool isFirstLine = true;
                    int currentSerial = serialNo++;

                    int rowIndex = 0;
                    int rowCount = grp.Count();

                    foreach (var r in grp)
                    {
                        bool isLastLine = rowIndex == rowCount - 1;

                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? currentSerial.ToString() : "");

                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? r.Date.ToString("dd-MM-yyyy") : "");

                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine
                                ? $"{r.VoucherType} / {r.VoucherId}"
                                : "");

                        table.Cell().Element(c => GroupCell(c, isLastLine))
                            .Text(isFirstLine ? r.Description : "");

                        table.Cell().Element(LineCell)
                            .Text(r.AccountName);

                        table.Cell().Element(LineCell).AlignRight()
                            .Text(r.Debit == 0 ? "" : r.Debit.ToString("N2"));

                        table.Cell().Element(LineCell).AlignRight()
                            .Text(r.Credit == 0 ? "" : r.Credit.ToString("N2"));

                        voucherDebit += r.Debit;
                        voucherCredit += r.Credit;
                        totalDebit += r.Debit;
                        totalCredit += r.Credit;

                        isFirstLine = false;
                        rowIndex++;
                    }

                    // 🔹 Voucher total
                    table.Cell().ColumnSpan(5)
                        .Element(LineCell)
                        .AlignRight()
                        .Text("Voucher Total :")
                        .Bold();

                    table.Cell().Element(LineCell).AlignRight()
                        .Text(voucherDebit.ToString("N2"))
                        .Bold();

                    table.Cell().Element(LineCell).AlignRight()
                        .Text(voucherCredit.ToString("N2"))
                        .Bold();
                }

                // 🔹 Grand total
                table.Cell().ColumnSpan(5)
                    .Element(LineCell)
                    .AlignRight()
                    .Text("TOTAL")
                    .Bold();

                table.Cell().Element(LineCell).AlignRight()
                    .Text(totalDebit.ToString("N2"))
                    .Bold();

                table.Cell().Element(LineCell).AlignRight()
                    .Text(totalCredit.ToString("N2"))
                    .Bold();
            });
        }


        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().AlignCenter().Text("VOUCHER REPORT")
                    .FontSize(14).Bold();

                col.Item().AlignCenter().Text(
                    $"From {_from:dd-MM-yyyy} To {_to:dd-MM-yyyy}" +
                    (string.IsNullOrEmpty(_voucherType)
                        ? ""
                        : $" | {_voucherType}")
                ).FontSize(10);

                col.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        static IContainer HeaderCell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(1)
             .PaddingHorizontal(4).PaddingVertical(3);

        static IContainer GroupCell(IContainer c, bool bottom) =>
            bottom
                ? c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
                     .PaddingHorizontal(4).PaddingVertical(2)
                : c.BorderLeft(0.5f).BorderRight(0.5f)
                     .PaddingHorizontal(4).PaddingVertical(2);

        static IContainer LineCell(IContainer c) =>
            c.BorderLeft(0.5f).BorderRight(0.5f).BorderBottom(0.5f)
             .PaddingHorizontal(4).PaddingVertical(2);

    }
}
