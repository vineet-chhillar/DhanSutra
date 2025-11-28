using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ItemForPurchaseInvoice
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ItemCode { get; set; }

        

        public string HsnCode { get; set; }

        

        public string UnitName { get; set; }

        public decimal GstPercent { get; set; }

    }
}
