using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class InvoiceReturnItemDto
    {
        public int InvoiceItemId { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string BatchNo { get; set; }
        public decimal OriginalQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal AvailableReturnQty { get; set; }
        public decimal Rate { get; set; }

        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal CgstPercent { get; set; }
        public decimal SgstPercent { get; set; }
        public decimal IgstPercent { get; set; }
    }

    public class InvoiceForReturnDto
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        public List<InvoiceReturnItemDto> ReturnItems { get; set; } = new List<InvoiceReturnItemDto>();
    }
}
