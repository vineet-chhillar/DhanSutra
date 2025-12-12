using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string GSTIN { get; set; }

        public string BillingAddress { get; set; }
        public string BillingCity { get; set; }
        public string BillingPincode { get; set; }
        public string BillingState { get; set; }

        public string ShippingAddress { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingPincode { get; set; }
        public string ShippingState { get; set; }

        public double OpeningBalance { get; set; }
        public double Balance { get; set; }
        public int CreditDays { get; set; }
        public double CreditLimit { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
    }


}
