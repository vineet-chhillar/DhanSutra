using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class LedgerReportDto
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }

        public string Cust_Supp_Name { get; set; }
        public int Cust_Supp_No { get; set; }
        public DateTime From { get; set; }    // yyyy-MM-dd
        public DateTime To { get; set; }      // yyyy-MM-dd
        public decimal OpeningBalance { get; set; } // Dr positive, Cr negative (we'll normalize in UI)
        public string OpeningSide { get; set; }     // "Dr" or "Cr"
        public List<LedgerRowDto> Rows { get; set; } = new List<LedgerRowDto>();
        public decimal ClosingBalance { get; set; } // positive
        public string ClosingSide { get; set; }     // "Dr" or "Cr"
    }
}
