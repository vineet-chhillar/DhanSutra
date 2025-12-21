using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class InvoiceSummaryDto
    {
        public long Id { get; set; }
        public string InvoiceNo { get; set; }
        public long InvoiceNum { get; set; }  // Optional
        
        public string CustomerName { get; set; }

        public decimal TotalAmount { get; set; }
    }

}
