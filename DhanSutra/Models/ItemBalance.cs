using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ItemBalance
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string BatchNo { get; set; }
        public double CurrentQty { get; set; }             // total for item
        public double CurrentQtyBatchWise { get; set; }    // quantity for this batch
        public string LastUpdated { get; set; }
    }

}
