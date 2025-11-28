using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class PurchaseItemForDateDto
    {
        public long ItemDetailsId { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime PurchaseDate { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public decimal Quantity { get; set; }
    }

}
