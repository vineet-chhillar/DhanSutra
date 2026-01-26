using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class BalanceSheetReportDto
    {
        //public DateTime AsOf { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public BalanceSheetGroup Assets { get; set; } = new BalanceSheetGroup();
        public BalanceSheetGroup Liabilities { get; set; } = new BalanceSheetGroup();
        public BalanceSheetGroup Capital { get; set; } = new BalanceSheetGroup();
    }
}
