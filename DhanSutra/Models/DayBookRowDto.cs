using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class DayBookRowDto
    {
        public DateTime EntryDate { get; set; }

        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public BigInteger VoucherId { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
    }

}
