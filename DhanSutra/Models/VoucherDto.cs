using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class VoucherDto
    {
        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public DateTime VoucherDate { get; set; }
        public string Narration { get; set; }
        public long? ReferenceId { get; set; }
        public List<VoucherLineDto> Lines { get; set; }
    }

    public class VoucherLineDto
    {
        public long AccountId { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }

    }

}
