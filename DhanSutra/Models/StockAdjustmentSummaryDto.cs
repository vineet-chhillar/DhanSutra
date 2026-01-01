using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class StockAdjustmentSummaryDto
    {
        public long AdjustmentId { get; set; }
        public string AdjustmentNo { get; set; }
        public string AdjustmentDate { get; set; }
        public string AdjustmentType { get; set; }
        public string Reason { get; set; }
    }

}
