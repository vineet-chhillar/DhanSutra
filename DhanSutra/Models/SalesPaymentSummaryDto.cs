using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesPaymentSummaryDto
    {
        public long InvoiceId { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
    }

}
