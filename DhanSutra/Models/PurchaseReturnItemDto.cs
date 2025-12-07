using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class PurchaseReturnItemDto
    {
        public long PurchaseReturnItemId { get; set; }
        public long PurchaseReturnId { get; set; }

        public long PurchaseItemId { get; set; }     // link to original PurchaseItem
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public string BatchNo { get; set; }
        public int BatchNum { get; set; }

        public decimal Qty { get; set; }             // Return Qty
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

    public class PurchaseReturnDto
    {
        public long PurchaseReturnId { get; set; }
        public long PurchaseId { get; set; }         // original invoice
        public long SupplierId { get; set; }

        public string ReturnNo { get; set; }
        public long ReturnNum { get; set; }
        public string ReturnDate { get; set; }       // "YYYY-MM-DD"

        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal RoundOff { get; set; }
        public decimal SubTotal { get; set; }

        public string Notes { get; set; }
        public string CreatedBy { get; set; }

        public List<PurchaseReturnItemDto> Items { get; set; } = new List<PurchaseReturnItemDto>();
    }

}
