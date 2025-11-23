using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SalesReturnSearchRowDto
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; }
        public string ReturnDate { get; set; }
        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
    }

}
