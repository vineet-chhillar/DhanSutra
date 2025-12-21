using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesInvoiceEditDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public long InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }

        public long CompanyProfileId { get; set; }

        public long? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string BillingState { get; set; }

        public string PaymentMode { get; set; }
        public string PaidVia { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        public List<SalesInvoiceEditItemDto> Items { get; set; } = new List<SalesInvoiceEditItemDto>();
    }

}
