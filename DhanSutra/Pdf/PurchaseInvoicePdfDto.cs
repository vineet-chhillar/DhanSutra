using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    namespace DhanSutra.Pdf
    {
        public class PurchaseInvoicePdfDto
        {
            public long PurchaseId { get; set; }
            public string InvoiceNo { get; set; }
            public long InvoiceNum { get; set; }
            public string InvoiceDate { get; set; }
            public long SupplierId { get; set; }

            public decimal TotalAmount { get; set; }
            public decimal TotalTax { get; set; }
            public decimal RoundOff { get; set; }
            public string Notes { get; set; }

            public List<PurchaseInvoicePdfItemDto> Items { get; set; }
        }

        public class PurchaseInvoicePdfItemDto
        {
            public long PurchaseItemId { get; set; }
            public long ItemId { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal NetRate { get; set; }

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
            public string Notes { get; set; }

            public int BatchNum { get; set; }
            public string BatchNo { get; set; }

            // OPTIONAL FIELDS
            public decimal? SalesPrice { get; set; }
            public decimal? Mrp { get; set; }
            public string Description { get; set; }
            public string MfgDate { get; set; }
            public string ExpDate { get; set; }
            public string ModelNo { get; set; }
            public string Brand { get; set; }
            public string Size { get; set; }
            public string Color { get; set; }
            public decimal? Weight { get; set; }
            public string Dimension { get; set; }
        }
    }

}
