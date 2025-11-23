using System;
using System.Collections.Generic;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;





namespace DhanSutra.Pdf
    {
        public class SalesReturnLoadDto
        {
            public int Id { get; set; }
            public string ReturnNo { get; set; }
            public int ReturnNum { get; set; }
            public DateTime ReturnDate { get; set; }
            public string InvoiceNo { get; set; }
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public string CustomerPhone { get; set; }
            public string CustomerState { get; set; }
            public string CustomerAddress { get; set; }

            public decimal SubTotal { get; set; }
            public decimal TotalTax { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal RoundOff { get; set; }
            public string Notes { get; set; }

            public List<SalesReturnItemForPrintDto> Items { get; set; }
        }
        public class SalesReturnItemForPrintDto
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public string BatchNo { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal GstPercent { get; set; }
            public decimal GstValue { get; set; }
            public decimal CgstPercent { get; set; }
            public decimal CgstValue { get; set; }
            public decimal SgstPercent { get; set; }
            public decimal SgstValue { get; set; }
            public decimal IgstPercent { get; set; }
            public decimal IgstValue { get; set; }
            public decimal LineSubTotal { get; set; }
            public decimal LineTotal { get; set; }
        }
    public class CompanyProfileSRDto
    {
        //public string CompanyName { get; set; }
        //public string AddressLine1 { get; set; }
        //public string AddressLine2 { get; set; }
        //public string City { get; set; }
        //public string State { get; set; }
        //public string Pincode { get; set; }
        //public string GSTIN { get; set; }
        //public string PAN { get; set; }
        //public string Email { get; set; }
        //public string Phone { get; set; }


        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string Country { get; set; }

        public string GSTIN { get; set; }
        public string PAN { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string IFSC { get; set; }
        public string BranchName { get; set; }

        public string InvoicePrefix { get; set; }
        public int InvoiceStartNo { get; set; }
        public int CurrentInvoiceNo { get; set; }

        public byte[] Logo { get; set; }

        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }

    }

    public class SalesReturnDocument : IDocument
        {
            private readonly SalesReturnLoadDto _sr;
            private readonly CompanyProfileSRDto _company;

            public SalesReturnDocument(SalesReturnLoadDto sr, CompanyProfileSRDto company)
            {
                _sr = sr;
                _company = company;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public DocumentSettings GetSettings() => new DocumentSettings
            {
                PdfA = true,
                CompressDocument = true
            };

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Margin(25);

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text("Thank you for shopping with us!");
                });
            }

            // ---------------- HEADER ----------------
            private void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(_company.CompanyName).SemiBold().FontSize(18);
                        col.Item().Text(_company.AddressLine1);
                        if (!string.IsNullOrWhiteSpace(_company.AddressLine2))
                            col.Item().Text(_company.AddressLine2);
                        col.Item().Text($"{_company.City}, {_company.State} - {_company.Pincode}");
                        col.Item().Text($"GSTIN: {_company.GSTIN}");
                        if (!string.IsNullOrWhiteSpace(_company.Phone))
                            col.Item().Text($"Phone: {_company.Phone}");
                    });

                    if (_company.Logo != null && _company.Logo.Length > 0)
                    {
                        row.ConstantItem(120)
                            .Height(60)
                            .Image(_company.Logo);
                    }
                });

                container.PaddingTop(10).BorderBottom(1).PaddingBottom(5)
                    .Text("SALES RETURN").SemiBold().FontSize(16).AlignCenter();
            }

            // ---------------- CONTENT ----------------
            private void ComposeContent(IContainer container)
            {
                container.PaddingTop(10).Column(col =>
                {
                    // Return + Customer Block
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Return No: {_sr.ReturnNo}");
                            c.Item().Text($"Return Date: {_sr.ReturnDate:dd-MM-yyyy}");
                            c.Item().Text($"Invoice No: {_sr.InvoiceNo}");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Customer: {_sr.CustomerName}");
                            if (!string.IsNullOrWhiteSpace(_sr.CustomerAddress))
                                c.Item().Text(_sr.CustomerAddress);
                            if (!string.IsNullOrWhiteSpace(_sr.CustomerPhone))
                                c.Item().Text($"Phone: {_sr.CustomerPhone}");
                        });
                    });

                    col.Item().PaddingVertical(10).BorderBottom(1);

                    // Items Table
                    col.Item().Element(ComposeItemsTable);

                    // Totals Section
                    col.Item().PaddingTop(8).AlignRight().Column(tot =>
                    {
                        tot.Item().Text($"SubTotal: {_sr.SubTotal:0.00}");
                        tot.Item().Text($"Total Tax: {_sr.TotalTax:0.00}");
                        tot.Item().Text($"Round Off: {_sr.RoundOff:0.00}");
                        tot.Item().Text($"Grand Total: {_sr.TotalAmount:0.00}").Bold();
                    });

                    // Notes
                    if (!string.IsNullOrWhiteSpace(_sr.Notes))
                    {
                        col.Item().PaddingTop(6).PaddingBottom(3).Text("Notes:").Bold();
                        col.Item().Text(_sr.Notes);
                    }
                });
            }

            // ---------------- ITEM TABLE ----------------
            private void ComposeItemsTable(IContainer container)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(); // Item
                        cols.RelativeColumn(0.9f); // Batch
                        cols.RelativeColumn(0.7f); // Qty
                        cols.RelativeColumn(0.8f); // Rate
                        cols.RelativeColumn(0.7f); // GST%
                        cols.RelativeColumn(0.9f); // Amount
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Text("Item").SemiBold();
                        header.Cell().Text("Batch").SemiBold();
                        header.Cell().Text("Qty").SemiBold();
                        header.Cell().Text("Rate").SemiBold();
                        header.Cell().Text("GST%").SemiBold();
                        header.Cell().Text("Amount").SemiBold();
                    });

                    // Rows
                    foreach (var x in _sr.Items)
                    {
                        table.Cell().Text(x.ItemId.ToString()); // Change to itemName if needed
                        table.Cell().Text(x.BatchNo);
                        table.Cell().Text($"{x.Qty:0.##}");
                        table.Cell().Text($"{x.Rate:0.##}");
                        table.Cell().Text($"{x.GstPercent:0.##}");
                        table.Cell().Text($"{x.LineTotal:0.00}");
                    }
                });
            }
        }
    }


