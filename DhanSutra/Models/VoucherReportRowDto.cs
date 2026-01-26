using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class VoucherReportRowDto
    {
        public long JournalId { get; set; }
        public long LineId { get; set; }

        public DateTime Date { get; set; }
        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public long VoucherId { get; set; }
        public string Description { get; set; }

        public long AccountId { get; set; }
        public string AccountName { get; set; }

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal? RunningBalance { get; set; }

    }


}
