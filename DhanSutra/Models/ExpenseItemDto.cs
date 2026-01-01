using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ExpenseItemDto
    {
        public long AccountId { get; set; }   // Expense account
        public decimal Amount { get; set; }
        public string Notes { get; set; }
    }

}
