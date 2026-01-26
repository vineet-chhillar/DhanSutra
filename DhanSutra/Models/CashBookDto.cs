using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class CashBookDto
    {
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal? RunningBalance { get; set; }
        public List<VoucherReportRowDto> Rows { get; set; }
    }

}
