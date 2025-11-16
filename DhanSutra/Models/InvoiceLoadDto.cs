using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class InvoiceLoadDto
    {
        public long Id { get; set; }

        public string InvoiceNo { get; set; }
        public int InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }

        public int CompanyProfileId { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string CustomerAddress { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        public List<InvoiceItemDto> Items { get; set; }
    }

}
