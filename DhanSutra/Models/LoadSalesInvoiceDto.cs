using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class LoadSalesInvoiceDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public long InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }

        public long CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string RefundMode { get; set; }

       

        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string PaidVia { get; set; }
        public List<LoadSalesInvoiceItemDto> Items { get; set; }
    }
}
