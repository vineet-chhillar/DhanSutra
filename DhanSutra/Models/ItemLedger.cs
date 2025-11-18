using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ItemLedger
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string BatchNo { get; set; }
        public string Date { get; set; }
        public string TxnType { get; set; }
        public string RefNo { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal NetRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
    }

}
