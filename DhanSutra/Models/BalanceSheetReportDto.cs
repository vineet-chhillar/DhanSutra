using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class BalanceSheetReportDto
    {
        public string AsOf { get; set; }
        public BalanceSheetGroup Assets { get; set; } = new BalanceSheetGroup();
        public BalanceSheetGroup Liabilities { get; set; } = new BalanceSheetGroup();
        public BalanceSheetGroup Capital { get; set; } = new BalanceSheetGroup();
    }
}
