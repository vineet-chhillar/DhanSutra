using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesReturnDto
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; }
        public int ReturnNum { get; set; }
        public DateTime ReturnDate { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public int CustomerId { get; set; }
        // Totals
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        public string Notes { get; set; }

        // Audit
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
       
        public List<SalesReturnItemDto> Items { get; set; } = new List<SalesReturnItemDto>();
    }
    public class SalesReturnItemDto
    {
        public int InvoiceItemId { get; set; }
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

        // optional balances (may be null if backend doesn't supply)
        public decimal AvailableStock { get; set; }
        public decimal BalanceBatchWise { get; set; }
    }
}
