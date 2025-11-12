using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ItemDetails
    {
        public int Item_Id { get; set; }
        public string HsnCode { get; set; }
        public string BatchNo { get; set; }
        public string refno { get; set; }
        public DateTime Date { get; set; }
        public double Quantity { get; set; }
        public double PurchasePrice { get; set; }

        public double DiscountPercent { get; set; }

        public double NetPurchasePrice { get; set; }

        public double Amount { get; set; }

        public double SalesPrice { get; set; }
        public double Mrp { get; set; }
        public string GoodsOrServices { get; set; }
        public string Description { get; set; }
        public string MfgDate { get; set; }
        public string ExpDate { get; set; }
        public string ModelNo { get; set; }
        public string Brand { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public string Weight { get; set; }
        public string Dimension { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
