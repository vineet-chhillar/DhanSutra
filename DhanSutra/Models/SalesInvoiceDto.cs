using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesInvoiceDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public long InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }
        public long CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string Notes { get; set; }

        public List<SalesInvoiceItemDto> Items { get; set; } = new List<SalesInvoiceItemDto>();
    }
    


}
