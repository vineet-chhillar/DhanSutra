using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ExpenseVoucherSummaryDto
    {
        public long ExpenseVoucherId { get; set; }
        public string VoucherNo { get; set; }
        public string Date { get; set; }

        public string Notes { get; set; }
        public string PaymentMode { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
    }

}
