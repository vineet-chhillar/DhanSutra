using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class IncomeVoucherSummaryDto
    {
        public long IncomeVoucherId { get; set; }
        public string VoucherNo { get; set; }
        public string Date { get; set; }

        public string Notes { get; set; }
        public string PaymentMode { get; set; }
        public decimal TotalAmount { get; set; }

        // For income we receive money (mirror of PaidAmount)
        public decimal ReceivedAmount { get; set; }
    }


}
