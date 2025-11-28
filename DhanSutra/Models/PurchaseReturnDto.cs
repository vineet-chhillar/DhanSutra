using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class PurchaseReturnDto
    {
        public long ReturnId { get; set; }

        public string ReturnNo { get; set; }   // NEW
        public long ReturnNum { get; set; }    // NEW

        public long ItemDetailsId { get; set; }
        public int ItemId { get; set; }
        public int SupplierId { get; set; }
        public DateTime ReturnDate { get; set; }

        public decimal Qty { get; set; }

        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal NetRate { get; set; }
        public string BatchNo { get; set; }

        public decimal GstPercent { get; set; }
        public decimal Amount { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal TotalAmount { get; set; }

        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
    }



}
