using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ProfitLossReportDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public List<ProfitLossRow> Income { get; set; }
        public List<ProfitLossRow> Expenses { get; set; }

        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetLoss { get; set; }

        public ProfitLossReportDto()
        {
            Income = new List<ProfitLossRow>();
            Expenses = new List<ProfitLossRow>();
        }
    }

}
