using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesReturnItemDetailDto
    {
        public long ReturnItemId { get; set; }
        public long InvoiceItemId { get; set; }
        public long ItemId { get; set; }

        public string ItemName { get; set; }
        public string BatchNo { get; set; }

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
    }

}
