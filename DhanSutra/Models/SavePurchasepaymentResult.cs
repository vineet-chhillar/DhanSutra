using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SavePurchasePaymentResult
    {
        public bool Success { get; set; }
        public decimal Amount { get; set; }
        public decimal NewPaidAmount { get; set; }
        public decimal NewBalanceAmount { get; set; }
    }

}
