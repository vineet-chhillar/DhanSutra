using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class InvoiceItemDto
    {
        public int ItemId { get; set; }                 // 0 if not linked to master
        public string ItemName { get; set; } = string.Empty;
        public string HsnCode { get; set; }
        public string BatchNo { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }             // Qty * Rate (after discount)
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }          // Amount * GstPercent / 100
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal Discount { get; set; }           // absolute discount amount (optional)

        /// <summary>
        /// Safe factory from JObject - tolerates missing fields.
        /// </summary>
        public static InvoiceItemDto FromJObject(JObject j)
        {
            if (j == null) return new InvoiceItemDto();

            return new InvoiceItemDto
            {
                ItemId = (int?)j["ItemId"] ?? 0,
                ItemName = (string)j["ItemName"] ?? string.Empty,
                HsnCode = (string)j["HsnCode"],
                BatchNo = (string)j["BatchNo"], 
                Qty = Convert.ToDecimal((double?)j["Qty"] ?? 0d),
                Unit = (string)j["Unit"],
                Rate = Convert.ToDecimal((double?)j["Rate"] ?? 0d),
                Amount = Convert.ToDecimal((double?)j["Amount"] ?? 0d),
                GstPercent = Convert.ToDecimal((double?)j["GstPercent"] ?? 0d),
                TaxAmount = Convert.ToDecimal((double?)j["TaxAmount"] ?? 0d),
                Cgst = Convert.ToDecimal((double?)j["Cgst"] ?? 0d),
                Sgst = Convert.ToDecimal((double?)j["Sgst"] ?? 0d),
                Igst = Convert.ToDecimal((double?)j["Igst"] ?? 0d),
                Discount = Convert.ToDecimal((double?)j["Discount"] ?? 0d)
            };
        }
    }

}
