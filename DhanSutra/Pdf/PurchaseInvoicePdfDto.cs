using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    namespace DhanSutra.Pdf
    {
    public class PurchaseInvoicePdfDto
    {
        public long PurchaseId { get; set; }
        public string InvoiceNo { get; set; }
        public long InvoiceNum { get; set; }
        public string InvoiceDate { get; set; }

        // Supplier Details
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierGSTIN { get; set; }
        public string SupplierAddress { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierState { get; set; }

        // Totals
        public decimal SubTotal { get; set; }
        public string CreatedBy { get; set; }
        public decimal SubTotalAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal RoundOff { get; set; }
        public string Notes { get; set; }

        public List<PurchaseInvoiceItemPdfDto> Items { get; set; }
    }
    
    
    }


