using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class OpeningStockHeaderDto
    {
        public string AsOnDate { get; set; }
        public string Notes { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class OpeningStockItemDto
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public string BatchNo { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Value { get; set; }
    }

}
