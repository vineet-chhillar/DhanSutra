using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class AccountDto
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }

        public long ParentAccountId { get; set; }
        

        public string NormalSide { get; set; }
        public double OpeningBalance { get; set; }
        public bool IsActive { get; set; }
    }

}
