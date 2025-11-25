using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SupplierDto
    {
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string GSTIN { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Pincode { get; set; }
        public double? OpeningBalance { get; set; }
        public double? Balance { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
    }

}
