using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class PurchaseItemDetailDto
    {
        public long ItemDetailsId { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public string HsnCode { get; set; }

        public string BatchNo { get; set; }          // NEW
        public decimal PurchasePrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal NetPurchasePrice { get; set; } // NEW — this is net rate
        public decimal Quantity { get; set; }
        public decimal AlreadyReturnedQty { get; set; }

        public string InvoiceNo { get; set; }
        public DateTime PurchaseDate { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public decimal GstPercent { get; set; }
    }


}
