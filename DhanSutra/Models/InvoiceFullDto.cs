using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class InvoiceFullDto
    {
        public int InvoiceNum { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }

        public int CompanyProfileId { get; set; }

        // Customer
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string CustomerAddress { get; set; }

        // Totals
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
        // ---------- NEW COMPANY PROFILE FIELDS ----------
        public byte[] CompanyLogo { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddressLine1 { get; set; }
        public string CompanyAddressLine2 { get; set; }
        public string CompanyCity { get; set; }
        public string CompanyState { get; set; }
        public string CompanyPincode { get; set; }
        public string CompanyCountry { get; set; }

        public string CompanyGstin { get; set; }
        public string CompanyPan { get; set; }

        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }

        public string CompanyBankName { get; set; }
        public string CompanyBankAccount { get; set; }
        public string CompanyIfsc { get; set; }
        public string CompanyBranchName { get; set; }
    }

}
