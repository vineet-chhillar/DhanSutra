using Newtonsoft.Json.Linq;
using System;

namespace DhanSutra.Models
{
    public class InvoiceItemDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int ItemId { get; set; }
        public string BatchNo { get; set; }
        public string HsnCode { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstValue { get; set; }
        public decimal CgstPercent { get; set; }
        public decimal CgstValue { get; set; }
        public decimal SgstPercent { get; set; }
        public decimal SgstValue { get; set; }
        public decimal IgstPercent { get; set; }
        public decimal IgstValue { get; set; }
        public decimal LineSubTotal { get; set; }
        public decimal LineTotal { get; set; }

        /// <summary>
        /// Safe factory to build InvoiceItemDto from a JObject (payload). Tolerant to missing properties.
        /// </summary>
        public static InvoiceItemDto FromJObject(JObject payload)
        {
            var dto = new InvoiceItemDto();
            if (payload == null) return dto;

            dto.Id = (int?)payload["Id"] ?? 0;
            dto.InvoiceId = (int?)payload["InvoiceId"] ?? 0;
            dto.ItemId = (int?)payload["ItemId"] ?? 0;
            dto.BatchNo = (string)payload["BatchNo"];
            dto.HsnCode = (string)payload["HsnCode"];
            dto.Qty = Convert.ToDecimal((double?)payload["Qty"] ?? 0d);
            dto.Rate = Convert.ToDecimal((double?)payload["Rate"] ?? 0d);
            dto.DiscountPercent = Convert.ToDecimal((double?)payload["DiscountPercent"] ?? 0d);
            dto.GstPercent = Convert.ToDecimal((double?)payload["GstPercent"] ?? 0d);
            dto.GstValue = Convert.ToDecimal((double?)payload["GstValue"] ?? 0d);
            dto.CgstPercent = Convert.ToDecimal((double?)payload["CgstPercent"] ?? 0d);
            dto.CgstValue = Convert.ToDecimal((double?)payload["CgstValue"] ?? 0d);
            dto.SgstPercent = Convert.ToDecimal((double?)payload["SgstPercent"] ?? 0d);
            dto.SgstValue = Convert.ToDecimal((double?)payload["SgstValue"] ?? 0d);
            dto.IgstPercent = Convert.ToDecimal((double?)payload["IgstPercent"] ?? 0d);
            dto.IgstValue = Convert.ToDecimal((double?)payload["IgstValue"] ?? 0d);
            dto.LineSubTotal = Convert.ToDecimal((double?)payload["LineSubTotal"] ?? 0d);
            dto.LineTotal = Convert.ToDecimal((double?)payload["LineTotal"] ?? 0d);

            return dto;
        }
    }
}
