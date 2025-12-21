using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesPaymentDto
    {
        public long InvoiceId { get; set; }
        public string PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }   // CASH / BANK / UPI
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
    }

}
