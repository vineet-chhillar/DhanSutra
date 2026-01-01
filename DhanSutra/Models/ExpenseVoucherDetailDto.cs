using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class ExpenseVoucherDetailDto
    {
        public long ExpenseVoucherId { get; set; }
        public string VoucherNo { get; set; }
        public string Date { get; set; }
        public string PaymentMode { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public List<ExpenseVoucherItemDto> Items { get; set; }
    }

    public class ExpenseVoucherItemDto
    {
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
    }

}
