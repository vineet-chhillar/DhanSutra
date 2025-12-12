using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ProfitLossReportDto
    {
        public string From { get; set; }
        public string To { get; set; }

        public List<ProfitLossRow> Income { get; set; } = new List<ProfitLossRow>();
        public List<ProfitLossRow> Expenses { get; set; } = new List<ProfitLossRow>();

        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }

        public decimal NetProfit { get; set; }   // positive means profit
        public decimal NetLoss { get; set; }     // positive means loss
    }
}
