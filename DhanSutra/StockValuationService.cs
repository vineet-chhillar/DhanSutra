using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace DhanSutra
{
    public class StockValuationRow
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public decimal OpeningQty { get; set; }
        public decimal OpeningValue { get; set; }
        public decimal InQty { get; set; }
        public decimal InValue { get; set; }
        public decimal OutQty { get; set; }   // qty sold (for the period)
        public decimal OutValue { get; set; } // cost recognized (COGS) for the period
        public decimal ClosingQty { get; set; }
        public decimal ClosingValue { get; set; } // valued by FIFO
    }

    public class StockValuationService
    {
        private readonly string _connectionString = "Data Source=billing.db;Version=3;BusyTimeout=5000;";
        
        public StockValuationService() 
        {
            // ✅ Step 1: Define a safe, user-visible folder
            string dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DhanSutra\\DhanSutra\\DhanSutraData"
            );

            // ✅ Step 2: Create folder if missing
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
                Console.WriteLine("📁 Created folder: " + dataFolder);
            }

            // ✅ Step 3: Full database path
            string dbFile = Path.Combine(dataFolder, "billing.db");

            // ✅ Step 4: Final connection string
            _connectionString = $"Data Source={dbFile};Version=3;";
            Console.WriteLine("📂 Database path: " + dbFile);

        }
        public class StockSummaryRow
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public decimal Qty { get; set; }
            public decimal FifoValue { get; set; }
            public decimal AvgCost { get; set; }
            public decimal LastPurchasePrice { get; set; }
            public decimal SellingPrice { get; set; }
            public decimal MarginPercent { get; set; }
            public decimal ReorderLevel { get; set; }
            public string Status { get; set; }
        }

        public List<StockSummaryRow> GetStockSummary(DateTime asOf)
        {
            var valuation = CalculateStockValuationFIFO(asOf); // we coded earlier
            var rows = new List<StockSummaryRow>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                foreach (var v in valuation)
                {
                    decimal avgCost = v.ClosingQty > 0 ? v.ClosingValue / v.ClosingQty : 0;
                    decimal lastPurchasePrice = 0;
                    decimal sellingPrice = 0;
                    decimal reorderLevel = 0;

                    // LAST PURCHASE PRICE
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT Rate 
                    FROM PurchaseItem 
                    WHERE ItemId=@id 
                    ORDER BY PurchaseItemId DESC LIMIT 1;";
                        cmd.Parameters.AddWithValue("@id", v.ItemId);
                        var o = cmd.ExecuteScalar();
                        if (o != null) lastPurchasePrice = Convert.ToDecimal(o);
                    }

                    // SELLING PRICE (from purchase details)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT salesPrice
                    FROM PurchaseItemDetails d
                    JOIN PurchaseItem p ON p.PurchaseItemId = d.PurchaseItemId
                    WHERE p.ItemId=@id AND d.salesPrice IS NOT NULL
                    ORDER BY d.PurchaseItemId DESC LIMIT 1;";
                        cmd.Parameters.AddWithValue("@id", v.ItemId);
                        var o = cmd.ExecuteScalar();
                        if (o != null) sellingPrice = Convert.ToDecimal(o);
                    }

                    // REORDER LEVEL (optional)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT ReorderLevel FROM Item WHERE Id=@id LIMIT 1;";
                        cmd.Parameters.AddWithValue("@id", v.ItemId);
                        var o = cmd.ExecuteScalar();
                        if (o != null) reorderLevel = Convert.ToDecimal(o);
                    }

                    // MARGIN
                    decimal margin = 0;
                    if (sellingPrice > 0 && avgCost > 0)
                        margin = (sellingPrice - avgCost) / avgCost * 100;

                    string status = v.ClosingQty <= reorderLevel ? "Low Stock" : "OK";

                    rows.Add(new StockSummaryRow
                    {
                        ItemId = v.ItemId,
                        ItemName = v.ItemName,
                        Qty = v.ClosingQty,
                        FifoValue = v.ClosingValue,
                        AvgCost = avgCost,
                        LastPurchasePrice = lastPurchasePrice,
                        SellingPrice = sellingPrice,
                        MarginPercent = margin,
                        ReorderLevel = reorderLevel,
                        Status = status
                    });
                }
            }

            return rows;
        }


        // Helper: load ledger entries up to asOf (inclusive)
        private List<dynamic> LoadLedgerEntriesUpTo(DateTime asOf)
        {
            var entries = new List<dynamic>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT Id, ItemId, BatchNo, Date, TxnType, RefNo, Qty, Rate, DiscountPercent, NetRate, TotalAmount
                    FROM ItemLedger
                    WHERE Date(Date) <= Date(@asOf)
                    ORDER BY Date(Date) ASC, Id ASC;
                ";
                    cmd.Parameters.AddWithValue("@asOf", asOf);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            entries.Add(new
                            {
                                Id = rd.GetInt64(0),
                                ItemId = rd.GetInt32(1),
                                BatchNo = rd.IsDBNull(2) ? "" : rd.GetString(2),
                                Date = rd.IsDBNull(3) ? "" : rd.GetString(3),
                                TxnType = rd.IsDBNull(4) ? "" : rd.GetString(4),
                                RefNo = rd.IsDBNull(5) ? "" : rd.GetString(5),
                                Qty = Convert.ToDecimal(rd.IsDBNull(6) ? 0.0 : rd.GetDouble(6)),
                                Rate = Convert.ToDecimal(rd.IsDBNull(7) ? 0.0 : rd.GetDouble(7)),
                                DiscountPercent = Convert.ToDecimal(rd.IsDBNull(8) ? 0.0 : rd.GetDouble(8)),
                                NetRate = Convert.ToDecimal(rd.IsDBNull(9) ? 0.0 : rd.GetDouble(9)),
                                TotalAmount = Convert.ToDecimal(rd.IsDBNull(10) ? 0.0 : rd.GetDouble(10))
                            });
                        }
                    }
                }
            }
            return entries;
        }

        // Public: calculate FIFO valuation. periodFrom/periodTo optional — if provided, OutValue is computed for that period.
        public List<StockValuationRow> CalculateStockValuationFIFO(
    DateTime asOfDate,
    DateTime? periodFrom = null,
    DateTime? periodTo = null
)
        {
            // Load entries up to "as of" date
            var entries = LoadLedgerEntriesUpTo(asOfDate);

            // Group by ItemId
            var byItem = entries
                .GroupBy(e => (int)e.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = new List<StockValuationRow>();

            bool IsInTxn(string tx) =>
                string.Equals(tx, "Purchase", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tx, "Sales Return", StringComparison.OrdinalIgnoreCase);

            bool IsOutTxn(string tx) =>
                string.Equals(tx, "Sale", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tx, "Purchase Return", StringComparison.OrdinalIgnoreCase);

            DateTime? pf = periodFrom;
            DateTime? pt = periodTo;

            foreach (var kv in byItem)
            {
                int itemId = kv.Key;
                var list = kv.Value;

                var queue = new LinkedList<(decimal qty, decimal unitCost)>();

                decimal openingQty = 0m, openingValue = 0m;
                decimal inQty = 0m, inValue = 0m;
                decimal outQty = 0m, outValue = 0m;

                foreach (var e in list)
                {
                    DateTime dt = DateTime.Parse(e.Date);

                    string tx = e.TxnType;
                    decimal qty = e.Qty;
                    bool isNegativeQty = qty < 0m;

                    if (IsInTxn(tx) && !isNegativeQty)
                    {
                        decimal unitCost =
                            e.NetRate != 0m
                                ? e.NetRate
                                : (e.Qty != 0m ? e.TotalAmount / e.Qty : 0m);

                        decimal qtyIn = qty;

                        if (pf.HasValue && dt < pf.Value)
                        {
                            openingQty += qtyIn;
                            openingValue += qtyIn * unitCost;
                        }
                        else
                        {
                            inQty += qtyIn;
                            inValue += qtyIn * unitCost;
                        }

                        queue.AddLast((qtyIn, unitCost));
                    }
                    else if (IsOutTxn(tx) && !isNegativeQty)
                    {
                        decimal qtyToConsume = Math.Abs(qty);
                        decimal consumedCost = 0m;

                        while (qtyToConsume > 0m && queue.Count > 0)
                        {
                            var first = queue.First.Value;
                            if (first.qty > qtyToConsume)
                            {
                                consumedCost += qtyToConsume * first.unitCost;
                                queue.First.Value = (first.qty - qtyToConsume, first.unitCost);
                                qtyToConsume = 0m;
                            }
                            else
                            {
                                consumedCost += first.qty * first.unitCost;
                                qtyToConsume -= first.qty;
                                queue.RemoveFirst();
                            }
                        }

                        if (pf.HasValue && dt < pf.Value)
                        {
                            openingQty -= Math.Abs(qty);
                            openingValue -= consumedCost;
                        }
                        else if (pf.HasValue && pt.HasValue && dt >= pf.Value && dt <= pt.Value)
                        {
                            outQty += Math.Abs(qty);
                            outValue += consumedCost;
                        }
                        else if (!pf.HasValue)
                        {
                            outQty += Math.Abs(qty);
                            outValue += consumedCost;
                        }
                    }
                    else if (qty < 0m)
                    {
                        decimal qtyToConsume = Math.Abs(qty);
                        decimal consumedCost = 0m;

                        while (qtyToConsume > 0m && queue.Count > 0)
                        {
                            var first = queue.First.Value;
                            if (first.qty > qtyToConsume)
                            {
                                consumedCost += qtyToConsume * first.unitCost;
                                queue.First.Value = (first.qty - qtyToConsume, first.unitCost);
                                qtyToConsume = 0m;
                            }
                            else
                            {
                                consumedCost += first.qty * first.unitCost;
                                qtyToConsume -= first.qty;
                                queue.RemoveFirst();
                            }
                        }

                        if (pf.HasValue && dt < pf.Value)
                        {
                            openingQty -= Math.Abs(qty);
                            openingValue -= consumedCost;
                        }
                        else if (pf.HasValue && pt.HasValue && dt >= pf.Value && dt <= pt.Value)
                        {
                            outQty -= Math.Abs(qty);
                            outValue -= consumedCost;
                        }
                    }
                }

                decimal closingQty = 0m, closingValue = 0m;
                foreach (var lot in queue)
                {
                    closingQty += lot.qty;
                    closingValue += lot.qty * lot.unitCost;
                }
                DatabaseService db = new DatabaseService();
                results.Add(new StockValuationRow
                {
                    ItemId = itemId,
                    ItemName = db.GetItemNameById(itemId),
                    OpeningQty = Math.Max(0, openingQty),
                    OpeningValue = Math.Max(0, openingValue),
                    InQty = Math.Max(0, inQty),
                    InValue = Math.Max(0, inValue),
                    OutQty = Math.Max(0, outQty),
                    OutValue = Math.Max(0, outValue),
                    ClosingQty = Math.Max(0, closingQty),
                    ClosingValue = Math.Max(0, closingValue)
                });
            }

            return results;
        }


        // Convenience: totals for UI or P&L/Balance sheet
        public (decimal ClosingStockTotal, decimal PeriodCogsTotal)
    ComputeTotalsFIFO(DateTime periodFrom, DateTime periodTo)
        {
            // Use periodTo directly (already DateTime)
            var asOf = periodTo;

            var rows = CalculateStockValuationFIFO(asOf, periodFrom, periodTo);

            decimal closing = rows.Sum(r => r.ClosingValue);
            decimal cogs = rows.Sum(r => r.OutValue);

            return (closing, cogs);
        }

    }
}
