using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesReturnDetailDto
    {
        public long ReturnId { get; set; }
        public long InvoiceId { get; set; }
        public long CustomerId { get; set; }

        public string ReturnNo { get; set; }
        public long ReturnNum { get; set; }
        public string ReturnDate { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }
        public string Notes { get; set; }

        public string CreatedBy { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string CustomerAddress { get; set; }

        public List<SalesReturnItemDetailDto> Items { get; set; } = new List<SalesReturnItemDetailDto>();
    }

}
