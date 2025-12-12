using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class TrialBalanceRowDto
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public string NormalSide { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public string ClosingSide { get; set; } // "Dr" or "Cr"
    }

}
