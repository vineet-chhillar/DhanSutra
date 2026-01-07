using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class AccountStatementDto
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }

        public string From { get; set; }
        public string To { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public List<AccountStatementRowDto> Rows { get; set; }
    }

    public class AccountStatementRowDto
    {
        public long JournalId { get; set; }
        public long LineId { get; set; }

        public string Date { get; set; }
        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public string Narration { get; set; }

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

}
