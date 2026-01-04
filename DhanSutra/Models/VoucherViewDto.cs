using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class VoucherViewDto
    {
        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public string VoucherDate { get; set; }
        public string Narration { get; set; }
        public List<VoucherLineViewDto> Lines { get; set; } = new List<VoucherLineViewDto>();
    }

    public class VoucherLineViewDto
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

}
