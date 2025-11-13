using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class InvoiceDto
    {
        // Header
        public int InvoiceNum { get; set; }                 // numeric counter (for internal use)
        public string InvoiceNo { get; set; }              // formatted (prefix + padded number)
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public int CompanyProfileId { get; set; } = 1;

        // Customer
        public int? CustomerId { get; set; }                // null for walk-in
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }

        // Totals
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        // Audit
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Lines
        public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();

        /// <summary>
        /// Safe factory to build InvoiceDto from a JObject (payload). Tolerant to missing properties.
        /// Use when req.Payload is a JObject.
        /// </summary>
        public static InvoiceDto FromJObject(JObject payload)
        {
            var dto = new InvoiceDto();
            if (payload == null) return dto;

            dto.InvoiceNum = (int?)payload["InvoiceNum"] ?? 0;
            dto.InvoiceNo = (string)payload["InvoiceNo"];
            // try to parse date (accept string or direct DateTime)
            if (payload["InvoiceDate"] != null)
            {
                if (DateTime.TryParse((string)payload["InvoiceDate"] ?? string.Empty, out var dt))
                    dto.InvoiceDate = dt;
                else
                {
                    // fallback if it's a numeric unix timestamp or other
                    try { dto.InvoiceDate = payload["InvoiceDate"].ToObject<DateTime>(); } catch { }
                }
            }

            dto.CompanyProfileId = (int?)payload["CompanyProfileId"] ?? 1;
            dto.CustomerId = (int?)payload["CustomerId"];
            dto.CustomerName = (string)payload["CustomerName"];
            dto.CustomerPhone = (string)payload["CustomerPhone"];
            dto.CustomerAddress = (string)payload["CustomerAddress"];

            dto.SubTotal = Convert.ToDecimal((double?)payload["SubTotal"] ?? 0d);
            dto.TotalTax = Convert.ToDecimal((double?)payload["TotalTax"] ?? 0d);
            dto.TotalAmount = Convert.ToDecimal((double?)payload["TotalAmount"] ?? 0d);
            dto.RoundOff = Convert.ToDecimal((double?)payload["RoundOff"] ?? 0d);

            dto.CreatedBy = (string)payload["CreatedBy"];
            if (payload["CreatedAt"] != null)
            {
                DateTime.TryParse((string)payload["CreatedAt"], out var createdAt);
                if (createdAt != default) dto.CreatedAt = createdAt;
            }

            // Items: accept array or null
            var arr = payload["Items"] as JArray;
            if (arr != null)
            {
                foreach (var it in arr)
                {
                    if (it is JObject jo)
                    {
                        dto.Items.Add(InvoiceItemDto.FromJObject(jo));
                    }
                }
            }

            return dto;
        }
    }
}

