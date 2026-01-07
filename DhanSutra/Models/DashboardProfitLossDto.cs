using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class DashboardProfitLossDto
    {
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit => Revenue - Expenses;
    }

}
