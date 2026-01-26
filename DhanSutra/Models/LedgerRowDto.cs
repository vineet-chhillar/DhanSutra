using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class LedgerRowDto
    {
              
            public long LineId { get; set; }
            public DateTime Date { get; set; }   // ⬅️ was string
            public string VoucherType { get; set; }
            public long VoucherId { get; set; }
            public string Narration { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal RunningBalance { get; set; }
            public string RunningSide { get; set; }
        

    }
}
