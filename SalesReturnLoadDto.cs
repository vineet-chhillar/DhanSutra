namespace DhanSutra.Pdf
{
    public class SalesReturnLoadDto
    {
        public long Id { get; set; }
        public string ReturnNo { get; set; }
        public int ReturnNum { get; set; }
        public DateTime ReturnDate { get; set; }
        public string InvoiceNo { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerState { get; set; }
        public string CustomerAddress { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RoundOff { get; set; }
        public string Notes { get; set; }
        public List<SalesReturnItemForPrintDto> Items { get; set; }
    }

    public class SalesReturnItemForPrintDto
    {
        public int ItemId { get; set; }
        public string BatchNo { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstValue { get; set; }
        public decimal CgstPercent { get; set; }
        public decimal CgstValue { get; set; }
        public decimal SgstPercent { get; set; }
        public decimal SgstValue { get; set; }
        public decimal IgstPercent { get; set; }
        public decimal IgstValue { get; set; }
        public decimal LineSubTotal { get; set; }
        public decimal LineTotal { get; set; }
    }
}