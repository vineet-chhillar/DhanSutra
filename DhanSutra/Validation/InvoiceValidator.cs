using DhanSutra.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Validation
{
    public class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }

        public ValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }
    }

    public static class InvoiceValidator
    {
        public static List<ValidationError> Validate(InvoiceDto invoice)
        {
            var errors = new List<ValidationError>();

            if (invoice == null)
            {
                errors.Add(new ValidationError("invoice", "Invoice data is missing."));
                return errors;
            }

            // -----------------------------
            // 1. Invoice Date (DateTime)
            // -----------------------------
            if (invoice.InvoiceDate == default)
            {
                errors.Add(new ValidationError("invoiceDate", "Invoice date is invalid."));
            }

            // -----------------------------
            // 2. Customer State (Required)
            // -----------------------------
            if (string.IsNullOrWhiteSpace(invoice.Customer.BillingState))
            {
                errors.Add(new ValidationError("customerState", "Customer state is required."));
            }

            // -----------------------------
            // 3. At Least One Item
            // -----------------------------
            if (invoice.Items == null || invoice.Items.Count == 0)
            {
                errors.Add(new ValidationError("lineItems", "Please add at least one item."));
                return errors;
            }

            // -----------------------------
            // 4. Total Amount > 0
            // -----------------------------
            if (invoice.TotalAmount <= 0)
            {
                errors.Add(new ValidationError("invoiceTotal", "Invoice total cannot be 0."));
            }

            // -----------------------------
            // 5. Per-Line Validations
            // -----------------------------
            for (int i = 0; i < invoice.Items.Count; i++)
            {
                var l = invoice.Items[i];
                string prefix = $"Line {i + 1}:";

                if (l == null)
                {
                    errors.Add(new ValidationError($"line_{i}", $"{prefix} Line is empty."));
                    continue;
                }

                // Item Id
                if (l.ItemId <= 0)
                    errors.Add(new ValidationError($"ItemId_{i}", $"{prefix} Invalid Item."));

                // Item Name
                if (string.IsNullOrWhiteSpace(l.ItemName))
                    errors.Add(new ValidationError($"ItemName_{i}", $"{prefix} Item name required."));

                // Batch No
                if (string.IsNullOrWhiteSpace(l.BatchNo))
                    errors.Add(new ValidationError($"BatchNo_{i}", $"{prefix} Batch cannot be empty."));

                // HSN
                if (string.IsNullOrWhiteSpace(l.HsnCode))
                    errors.Add(new ValidationError($"HsnCode_{i}", $"{prefix} HSN code required."));

                // Quantity
                if (l.Qty <= 0)
                {
                    errors.Add(new ValidationError($"Qty_{i}", $"{prefix} Quantity must be greater than 0."));
                }
                else
                {
                    // Stock limits (optional)
                    if(l.AvailableStock != null && l.Qty > l.AvailableStock)
                        errors.Add(new ValidationError($"AvailableStock_{i}",
                            $"{prefix} Qty exceeds available stock ({l.AvailableStock})."));

                    if (l.BalanceBatchWise != null && l.Qty > l.BalanceBatchWise)
                        errors.Add(new ValidationError($"BatchStock_{i}",
                            $"{prefix} Qty exceeds batch stock ({l.BalanceBatchWise})."));
                }

                // Rate
                if (l.Rate <= 0)
                    errors.Add(new ValidationError($"Rate_{i}", $"{prefix} Rate must be greater than 0."));

                // Discount 0–100
                if (l.DiscountPercent < 0 || l.DiscountPercent > 100)
                    errors.Add(new ValidationError($"Discount_{i}", $"{prefix} Discount must be 0–100%."));

                // GST 0–28
                if (l.GstPercent < 0 || l.GstPercent > 28)
                    errors.Add(new ValidationError($"GstPercent_{i}", $"{prefix} GST % must be 0–28."));
            }

            return errors;
        }
    }
}
