using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class CompanyProfile
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string Country { get; set; }

        public string GSTIN { get; set; }
        public string PAN { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string IFSC { get; set; }
        public string BranchName { get; set; }

        public string InvoicePrefix { get; set; }
        public int InvoiceStartNo { get; set; }
        public int CurrentInvoiceNo { get; set; }

        public byte[] Logo { get; set; }

        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
    }

}
