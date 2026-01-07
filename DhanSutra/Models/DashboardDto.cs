using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class DashboardDto
    {
        // Summary cards
        public decimal CashBalance { get; set; }
        public decimal BankBalance { get; set; }
        public decimal TotalReceivable { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal TodaySales { get; set; }
        public decimal TodayPurchase { get; set; }

        // Profit snapshot
        public decimal MonthlyRevenue { get; set; }
        public decimal MonthlyExpense { get; set; }
        public decimal MonthlyProfit { get; set; }

        // Charts
        public List<SalesPurchaseChartDto> SalesPurchaseChart { get; set; }
        public List<CashBankTrendDto> CashBankTrend { get; set; }
    }

    public class SalesPurchaseChartDto
    {
        public string Label { get; set; }   // Week / Date
        public decimal Sales { get; set; }
        public decimal Purchase { get; set; }
    }

    public class CashBankTrendDto
    {
        public string Date { get; set; }
        public decimal Cash { get; set; }
        public decimal Bank { get; set; }
    }

}
