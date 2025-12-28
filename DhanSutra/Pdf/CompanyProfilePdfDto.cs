using DhanSutra.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Pdf
{
    public class CompanyProfilePdfDto
    {
        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
        public string GSTIN { get; set; }
        public string PAN { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string IFSC { get; set; }
        public string BranchName { get; set; }

        public string InvoicePrefix { get; set; }
        public long? InvoiceStartNo { get; set; }
        public long? CurrentInvoiceNo { get; set; }

        public byte[] Logo { get; set; }   // Logo in bytes
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }

        // ===== Constructor that accepts your CompanyProfile model =====
        public CompanyProfilePdfDto(CompanyProfile model)
        {
            CompanyName = model.CompanyName;
            AddressLine1 = model.AddressLine1;
            AddressLine2 = model.AddressLine2;
            City = model.City;
            State = model.State;
            Pincode = model.Pincode;
            GSTIN = model.GSTIN;
            PAN = model.PAN;
            Email = model.Email;
            Phone = model.Phone;

            BankName = model.BankName;
            BankAccount = model.BankAccount;
            IFSC = model.IFSC;
            BranchName = model.BranchName;

            InvoicePrefix = model.InvoicePrefix;
            InvoiceStartNo = model.InvoiceStartNo;
            CurrentInvoiceNo = model.CurrentInvoiceNo;

            Logo = model.Logo;
            CreatedBy = model.CreatedBy;
            CreatedAt = model.CreatedAt;
        }
    }

}
