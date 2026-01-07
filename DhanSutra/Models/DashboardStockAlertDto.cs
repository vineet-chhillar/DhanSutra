using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class DashboardStockAlertDto
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public int CurrentQty { get; set; }
        public string Status { get; set; } // LOW | OUT
    }

}
