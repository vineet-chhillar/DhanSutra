using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class BalanceSheetGroup
    {
        public string GroupName { get; set; }
        public List<ProfitLossRow> Rows { get; set; } = new List<ProfitLossRow>();
        public decimal Total { get; set; }
    }

}
