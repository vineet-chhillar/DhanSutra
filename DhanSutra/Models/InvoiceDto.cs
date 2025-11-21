using Newtonsoft.Json;
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
        public int InvoiceNum { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public int CompanyProfileId { get; set; } = 1;

        // Customer (single object)
        public CustomerDto Customer { get; set; }

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

        public static InvoiceDto FromJObject(JObject payload)
        {
            var dto = new InvoiceDto();
            if (payload == null) return dto;

            dto.InvoiceNum = (int?)payload["InvoiceNum"] ?? 0;
            dto.InvoiceNo = (string)payload["InvoiceNo"];

            // Parse date
            if (payload["InvoiceDate"] != null)
            {
                if (DateTime.TryParse((string)payload["InvoiceDate"], out var dt))
                    dto.InvoiceDate = dt;
            }

            dto.CompanyProfileId = (int?)payload["CompanyProfileId"] ?? 1;

            // -------------------------
            // CUSTOMER MAPPING (CORRECT)
            // -------------------------
            if (payload["Customer"] is JObject cust)
            {
                dto.Customer = new CustomerDto
                {
                    Id = (int?)cust["Id"] ?? 0,
                    Name = (string)cust["Name"],
                    Phone = (string)cust["Phone"],
                    State = (string)cust["State"],
                    Address = (string)cust["Address"]
                };
            }

            // Totals
            dto.SubTotal = Convert.ToDecimal((double?)payload["SubTotal"] ?? 0);
            dto.TotalTax = Convert.ToDecimal((double?)payload["TotalTax"] ?? 0);
            dto.TotalAmount = Convert.ToDecimal((double?)payload["TotalAmount"] ?? 0);
            dto.RoundOff = Convert.ToDecimal((double?)payload["RoundOff"] ?? 0);
            
            dto.CreatedBy = (string)payload["CreatedBy"];

            if (payload["CreatedAt"] != null)
            {
                if (DateTime.TryParse((string)payload["CreatedAt"], out var createdAt))
                    dto.CreatedAt = createdAt;
            }

            

            // Items
            if (payload["Items"] is JArray arr)
            {
                foreach (var it in arr)
                    if (it is JObject jo)
                        dto.Items.Add(InvoiceItemDto.FromJObject(jo));
            }

            

            return dto;
        }
    }

}

