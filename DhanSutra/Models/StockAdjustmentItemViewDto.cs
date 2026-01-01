using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class StockAdjustmentDetailDto
    {
        public string AdjustmentNo { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public List<StockAdjustmentItemViewDto> Items { get; set; }
    }

    public class StockAdjustmentItemViewDto
    {
        public string ItemName { get; set; }
        public string BatchNo { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal ValueImpact { get; set; }
    }

}
