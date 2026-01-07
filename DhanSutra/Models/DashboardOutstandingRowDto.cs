using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class DashboardOutstandingRowDto
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; } // +ve = Receivable, -ve = Payable
    }

}
