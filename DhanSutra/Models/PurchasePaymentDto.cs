using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class PurchasePaymentDto
    {
        public long PurchaseId { get; set; }
        public string PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } // CASH / BANK / UPI
        
        public string Notes { get; set; }
        public string CreatedBy { get; set; }

        public long SupplierAccountId { get; set; }   // Supplier ledger
        
    }

}
