using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class CreateInvoiceDto
    {
        public string InvoiceNo { get; set; }
        public long InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }
        public int CompanyId { get; set; }
        public string PaymentMode { get; set; }  // CASH / BANK / CREDIT

        public decimal PaidAmount { get; set; }
        public CustomerDto Customer { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        public string CreatedBy { get; set; }
        public string PaidVia { get; set; }
        public List<InvoiceItemDto> Items { get; set; }
    }

}
