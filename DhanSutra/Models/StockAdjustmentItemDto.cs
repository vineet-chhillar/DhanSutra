using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class StockAdjustmentItemDto
    {
        public long ItemId { get; set; }
        public string BatchNo { get; set; }
        public decimal AdjustQty { get; set; }
        public decimal Rate { get; set; }
    }

}
