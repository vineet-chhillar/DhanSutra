using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class SaveSalesReturnResult
    {
        public bool Success { get; set; }
        public long ReturnId { get; set; }
        public string Message { get; set; }
    }

}
