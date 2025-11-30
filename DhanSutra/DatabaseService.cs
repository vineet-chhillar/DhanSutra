using Dapper;
using DhanSutra.Models;
using DhanSutra.Pdf;
using DhanSutra.Validation;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace DhanSutra
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=billing.db;Version=3;BusyTimeout=5000;";

        public DatabaseService()
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

        public IEnumerable<DhanSutra.Models.Item> GetItems()
        {
            using (var connection = new SQLiteConnection(_connectionString))
                return connection.Query<DhanSutra.Models.Item>("SELECT i.Id, i.Name, i.ItemCode,i.hsnCode, c.CategoryName, \r\n      " +
                    " u.UnitName, g.GstPercent, i.Description, i.[Date]\r\nFROM Item i\r\nLEFT JOIN CategoryMaster c" +
                    " ON i.CategoryId = c.Id\r\nLEFT JOIN UnitMaster u ON i.UnitId = u.Id\r\nLEFT JOIN GstMaster g ON i.GstId = g.Id;");
        }
        public IEnumerable<ItemForInvoice> GetItemsForInvoice()
        {
            using (var connection = new SQLiteConnection(_connectionString))
                
                return connection.Query<ItemForInvoice>("select i.Id, i.Name, i.ItemCode, d.batchno, \r\n      " +
                    " i.hsncode, d.salesprice , u.unitname, g.gstpercent \r\nfrom itemdetails d\r\ninner join item i" +
                    " on i.id=d.item_id\r\nLEFT JOIN UnitMaster u ON i.UnitId = u.Id\r\nLEFT JOIN GstMaster g ON i.GstId = g.Id\r\n order by i.id;");
        }
        public IEnumerable<ItemForPurchaseInvoice> GetItemsForPurchaseInvoice()
        {
            using (var connection = new SQLiteConnection(_connectionString))
                return connection.Query<ItemForPurchaseInvoice>("select i.id AS Id, i.Name, i.ItemCode, i.hsncode, u.unitname, g.gstpercent\r\nfrom item i LEFT JOIN UnitMaster u ON i.UnitId = u.Id LEFT JOIN GstMaster g ON i.GstId = g.Id order by i.id;");
        }
        public IEnumerable<ItemDetails> GetItemDetails(int itemId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                Console.Write(itemId.ToString());
                return connection.Query<ItemDetails>(
           "SELECT itemdetails.*,suppliers.suppliername as SupplierName FROM ItemDetails\r\nleft join suppliers on suppliers.supplierid=itemdetails.supplierid\r\nWHERE item_id = @ItemId\r\n",
           new { ItemId = itemId }
           );
            }
        }
        public void AddItem(DhanSutra.Models.Item item)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                // 🧾 Log what we are actually inserting
                Console.WriteLine("📥 AddItem() received:");
                Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
                connection.Execute(
                "INSERT INTO Item (name, itemcode, hsnCode,categoryid,[date], description, unitid, gstid,createdby,createdat) " +
                "VALUES" +
                " (@Name, @ItemCode,@HsnCode, @CategoryId,@Date, @Description, @UnitId, @GstId,@CreatedBy,@CreatedAt)", item);
            }
        }

        public bool AddItemDetails(ItemDetails details, SQLiteConnection conn, SQLiteTransaction txn)
        {
            try
            {
                // Debug log
                Console.WriteLine("📥 AddItemDetails() received:");
                Console.WriteLine(JsonConvert.SerializeObject(details, Formatting.Indented));

                // ---------------------------
                // 🛑 STEP 1: Validate
                // ---------------------------
                var validationErrors = ValidateInventoryDetails(details);

                if (validationErrors.Count > 0)
                {
                    // Log validation errors
                    Console.WriteLine("❌ Validation failed:");
                    foreach (var err in validationErrors)
                    {
                        Console.WriteLine(" - " + err);
                    }

                    // ❗ Since function must return bool → return false on validation failure
                    return false;
                }

                // ---------------------------
                // 🟢 STEP 2: Insert if valid
                // ---------------------------
                string sql = @"
            INSERT INTO ItemDetails 
                (
                    item_id,
                     
                    batchNo,
                    refno,
                    [Date],
                    quantity,
                    purchasePrice,
                    discountPercent,
                    netPurchasePrice, 
                    amount,
                    salesPrice,
                    mrp, 
                    goodsOrServices,
                    description, 
                    mfgdate,
                    expdate,
                    modelno, 
                    brand, 
                    size,
                    color,
                    weight,
                    dimension,
                    createdby,
                    createdat,
                    SupplierId
                )
            VALUES 
                (
                    @Item_Id,
                    @BatchNo,
                    @refno,
                    @Date,
                    @Quantity,
                    @PurchasePrice,
                    @DiscountPercent, 
                    @NetPurchasePrice,
                    @Amount,
                    @SalesPrice,
                    @Mrp,
                    @GoodsOrServices,
                    @Description, 
                    @MfgDate,
                    @ExpDate,
                    @ModelNo,
                    @Brand,
                    @Size,
                    @Color,
                    @Weight,
                    @Dimension,
                    @CreatedBy,
                    @CreatedAt,
                    @SupplierId
                );
        ";

                int rowsAffected = conn.Execute(sql, details, transaction: txn);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error inserting ItemDetails: " + ex.Message);
                return false;
            }
        }



        public string GetItemNameById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                // 🧠 Query the database for the item ID
                string query = "SELECT name FROM Item WHERE id = @id LIMIT 1";

                var result = connection.ExecuteScalar<object>(query, new { id = id });

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToString(result);
                }
                else
                {
                    Console.WriteLine($"⚠️ Item not found: {id}");
                    return null; // Or -1 if you prefer an integer default
                }
            }
        }

        public string GetCategoryById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                // 🧠 Query the database for the item ID
                string query = "SELECT * FROM CategoryMaster WHERE id = @id LIMIT 1";

                var result = connection.ExecuteScalar<object>(query, new { id = id });

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToString(result);
                }
                else
                {
                    Console.WriteLine($"⚠️ Category not found: {id}");
                    return null; // Or -1 if you prefer an integer default
                }
            }
        }

        public string GetUnitNameById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                // 🧠 Query the database for the item ID
                string query = "SELECT * FROM unitmaster WHERE id = @id LIMIT 1";

                var result = connection.ExecuteScalar<object>(query, new { id = id });

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToString(result);
                }
                else
                {
                    Console.WriteLine($"⚠️ Unit not found: {id}");
                    return null; // Or -1 if you prefer an integer default
                }
            }
        }
        public string GetGSTById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                // 🧠 Query the database for the item ID
                string query = "SELECT * FROM gstmaster WHERE id = @id LIMIT 1";

                var result = connection.ExecuteScalar<object>(query, new { id = id });

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToString(result);
                }
                else
                {
                    Console.WriteLine($"⚠️ GST not found: {id}");
                    return null; // Or -1 if you prefer an integer default
                }
            }
        }

        public List<CategoryMaster> GetAllCategories()
        {
            var list = new List<CategoryMaster>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT id, categoryname, description FROM CategoryMaster ORDER BY CategoryName ASC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CategoryMaster
                        {
                            Id = reader.GetInt32(0),
                            CategoryName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                }
            }

            return list;
        }
        public List<UnitMaster> GetAllUnits()
        {
            var list = new List<UnitMaster>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT id,unitname FROM UnitMaster ORDER BY UnitName ASC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new UnitMaster
                        {
                            Id = reader.GetInt32(0),
                            UnitName = reader.GetString(1),

                        });
                    }
                }
            }

            return list;
        }

        public List<GstMaster> GetAllGst()
        {
            var list = new List<GstMaster>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT id,gstpercent FROM GstMaster ORDER BY Id ASC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new GstMaster
                        {
                            Id = reader.GetInt32(0),
                            GstPercent = reader.GetString(1),

                        });
                    }
                }
            }

            return list;
        }

        public bool DeleteItemIfNoInventory(int itemId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // Optional: ensure foreign keys are enforced (good practice)
                using (var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

                string deleteQuery = @"
                DELETE FROM Item
                WHERE Id = @ItemId
                AND NOT EXISTS (SELECT 1 FROM ItemDetails WHERE item_id = @ItemId);
            ";

                using (var cmd = new SQLiteCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    // rowsAffected = 1 means deleted successfully
                    // rowsAffected = 0 means related inventory exists or item doesn’t exist
                    return rowsAffected > 0;
                }
            }
        }

        public List<object> SearchItems(string queryText)
        {
            var items = new List<object>();

            string sql = @"
            SELECT 
                i.id,
                i.name,
                i.itemcode,
                i.hsnCode,
                i.date,
                i.description,
                c.id as CategoryId,
                c.categoryname AS CategoryName,
                u.id as UnitId,                
                u.UnitName AS UnitName,
                g.id as GstId,
                g.gstpercent AS GstPercent
            FROM Item i
            LEFT JOIN categoryMaster c ON i.categoryId = c.Id
            LEFT JOIN UnitMaster u ON i.unitid = u.Id
            LEFT JOIN GstMaster g ON i.gstid = g.Id
            WHERE 
                i.name LIKE @query
                OR i.itemcode LIKE @query
                OR i.description LIKE @query
                OR c.categoryname LIKE @query
                OR u.unitname LIKE @query
                OR g.gstpercent LIKE @query
            ORDER BY i.name;
        ";

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@query", "%" + queryText + "%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new
                            {
                                Id = reader["id"],
                                Name = reader["name"],
                                ItemCode = reader["itemcode"],
                                HsnCode = reader["hsnCode"],
                                Date = reader["date"],
                                Description = reader["description"],
                                CategoryId = reader["CategoryId"],
                                CategoryName = reader["CategoryName"],
                                UnitId = reader["UnitId"],
                                UnitName = reader["UnitName"],
                                GstId = reader["GstId"],
                                GstPercent = reader["GstPercent"]
                            });
                        }
                    }
                }
            }

            return items;
        }
        public bool UpdateItem(int id, string name, string itemCode, string hsncode, int? categoryId, string date, string description, int? unitId, int? gstId)
        {
            string sql = @"
        UPDATE Item
        SET 
            name = @name,
            itemcode = @itemCode,
            hsnCode=@hsncode,
            categoryId = @categoryId,
            date = @date,
            description = @description,
            unitid = @unitId,
            gstid = @gstId
        WHERE id = @id;
    ";

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@itemCode", itemCode);
                    cmd.Parameters.AddWithValue("@hsncode", hsncode);

                    cmd.Parameters.Add("@date", System.Data.DbType.DateTime).Value = string.IsNullOrEmpty(date) ? (object)DBNull.Value : DateTime.Parse(date);
                    cmd.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                    cmd.Parameters.Add("@categoryId", DbType.Int32).Value = categoryId == null ? (object)DBNull.Value : (object)categoryId;
                    cmd.Parameters.Add("@unitId", DbType.Int32).Value =
                        unitId == null ? (object)DBNull.Value : (object)unitId;
                    cmd.Parameters.Add("@gstId", DbType.Int32).Value =
                        gstId == null ? (object)DBNull.Value : (object)gstId;

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }
        public List<DhanSutra.Models.Item> GetItemList()
        {
            var list = new List<DhanSutra.Models.Item>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT id,name FROM Item ORDER BY name ASC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new DhanSutra.Models.Item
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),

                        });
                    }
                }
            }

            return list;
        }
        // ✅ SEARCH INVENTORY
        // ✅ Search
        public List<JObject> SearchInventory(string queryText)
        {
            var list = new List<JObject>();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                SELECT 
    Item_Id,    BatchNo,    refno,    Date,    Quantity,    PurchasePrice,    discountPercent,    netPurchasePrice,    amount,
    SalesPrice,
    Mrp,
    GoodsOrServices,
    Description,
    MfgDate,
    ExpDate,
    ModelNo,
    Brand,
    Size,
    Color,
    Weight,
    Dimension,
    suppliers.suppliername,
suppliers.supplierId
FROM ItemDetails
left join suppliers on suppliers.supplierid=itemdetails.supplierid
WHERE 
     item_id=@query
                     ORDER BY Date DESC;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@query", queryText);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new JObject();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string colName = reader.GetName(i);
                                    object value = reader.IsDBNull(i) ? "" : reader.GetValue(i);
                                    row[colName] = JToken.FromObject(value);
                                }

                                list.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SearchInventory: {ex.Message}");
            }

            return list;
        }


        public bool UpdateInventoryRecord(
           SQLiteConnection conn,
           SQLiteTransaction tran,
           string itemId, string batchNo, string refno, string date,
           string quantity, string purchasePrice, string discountPercent,
           string netpurchasePrice, string amount, string salesPrice, string mrp,
           string goodsOrServices, string description, string mfgDate, string expDate,
           string modelNo, string brand, string size, string color, string weight,
           string dimension, string invbatchno, int supplierid)
        {
            try
            {
                string query = @"
            UPDATE ItemDetails
            SET 
                batchNo = @BatchNo,
                refno=@refno,
                date = @Date,
                quantity = @Quantity,
                purchasePrice = @PurchasePrice,
                discountPercent=@DiscountPercent,
                netpurchasePrice= @NetPurchasePrice,
                amount= @Amount,
                salesPrice = @SalesPrice,
                mrp = @Mrp,
                goodsOrServices = @GoodsOrServices,
                description = @Description,
                mfgdate = @MfgDate,
                expdate = @ExpDate,
                modelno = @ModelNo,
                brand = @Brand,
                size = @Size,
                color = @Color,
                weight = @Weight,
                dimension = @Dimension,
supplierid=@SupplierId
            WHERE item_Id = @ItemId 
              AND batchNo = @invbatchno;
        ";

                using (var cmd = new SQLiteCommand(query, conn, tran))
                {

                    cmd.Parameters.AddWithValue("@Date", date);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
                    cmd.Parameters.AddWithValue("@DiscountPercent", discountPercent);
                    cmd.Parameters.AddWithValue("@NetPurchasePrice", netpurchasePrice);
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@SalesPrice", salesPrice);
                    cmd.Parameters.AddWithValue("@Mrp", mrp);
                    cmd.Parameters.AddWithValue("@GoodsOrServices", goodsOrServices);
                    cmd.Parameters.AddWithValue("@Description", description);
                    cmd.Parameters.AddWithValue("@MfgDate", mfgDate);
                    cmd.Parameters.AddWithValue("@ExpDate", expDate);
                    cmd.Parameters.AddWithValue("@ModelNo", modelNo);
                    cmd.Parameters.AddWithValue("@Brand", brand);
                    cmd.Parameters.AddWithValue("@Size", size);
                    cmd.Parameters.AddWithValue("@Color", color);
                    cmd.Parameters.AddWithValue("@Weight", weight);
                    cmd.Parameters.AddWithValue("@Dimension", dimension);

                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.Parameters.AddWithValue("@BatchNo", batchNo);
                    cmd.Parameters.AddWithValue("@refno", refno);
                    cmd.Parameters.AddWithValue("@invbatchno", invbatchno);
                    cmd.Parameters.AddWithValue("@SupplierId", supplierid);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating inventory: " + ex.Message);
                return false;
            }
        }


        public bool UpdateItemLedger(
       SQLiteConnection conn,
       SQLiteTransaction tran,
       string itemId,
       string batchNo,
       string refno,
       string date,
       string quantity,
       string purchasePrice,
       string discountPercent,
       string netpurchasePrice,
       string amount,
       string description,
       string invbatchno)
        {
            try
            {
                string query = @"
            UPDATE ItemLedger
            SET 
                BatchNo = @BatchNo,
                refno=@refno,
                date = @Date,
                Qty = @Quantity,
                Rate = @PurchasePrice,
                DiscountPercent=@DiscountPercent,
                NetRate= @NetPurchasePrice,
                TotalAmount= @Amount,
                Remarks = @Description
            WHERE ItemId = @ItemId 
              AND BatchNo = @invbatchno;
        ";

                using (var cmd = new SQLiteCommand(query, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@Date", date);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
                    cmd.Parameters.AddWithValue("@DiscountPercent", discountPercent);
                    cmd.Parameters.AddWithValue("@NetPurchasePrice", netpurchasePrice);
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@Description", description);

                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.Parameters.AddWithValue("@BatchNo", batchNo);
                    cmd.Parameters.AddWithValue("@refno", refno);
                    cmd.Parameters.AddWithValue("@invbatchno", invbatchno);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating item ledger: " + ex.Message);
                return false;
            }
        }


        public bool UpdateItemBalanceForBatchNo(
    SQLiteConnection conn,
    SQLiteTransaction tran,
    string itemId,
    string batchNo,
    string invbatchno)
        {
            try
            {
                string query = @"
            UPDATE ItemBalance
            SET 
                BatchNo = @BatchNo
            WHERE ItemId = @ItemId 
              AND BatchNo = @invbatchno;
        ";

                using (var cmd = new SQLiteCommand(query, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    cmd.Parameters.AddWithValue("@BatchNo", batchNo);
                    cmd.Parameters.AddWithValue("@invbatchno", invbatchno);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating item balance: " + ex.Message);
                return false;
            }
        }


        public JObject GetLastItemWithInventory()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(@"
            SELECT i.Item_Id, it.Name 
FROM itemdetails i
JOIN Item it ON i.Item_Id = it.Id
ORDER BY i.CreatedAt DESC
LIMIT 1;", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new JObject
                            {
                                ["Item_Id"] = reader["Item_Id"].ToString(),
                                ["ItemName"] = reader["Name"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }
        public bool UpdateItemBalance(ItemLedger entry, SQLiteConnection conn, SQLiteTransaction txn)
        {
            try
            {
                // 1️⃣ Insert or update batch-wise balance
                string sql = @"
        INSERT INTO ItemBalance (ItemId, BatchNo, CurrentQtyBatchWise, CurrentQty)
        VALUES (@ItemId, @BatchNo, @Qty, @Qty)
        ON CONFLICT(ItemId, BatchNo)
        DO UPDATE SET 
            CurrentQtyBatchWise = CurrentQtyBatchWise + excluded.CurrentQtyBatchWise,
            LastUpdated = datetime('now','localtime');
        ";

                using (var cmd = new SQLiteCommand(sql, conn, txn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.Parameters.AddWithValue("@BatchNo", entry.BatchNo ?? "");
                    cmd.Parameters.AddWithValue("@Qty", entry.Qty);
                    cmd.ExecuteNonQuery();
                }

                // 2️⃣ Update total (CurrentQty) only for the latest record
                string sqlTotal = @"
        UPDATE ItemBalance
        SET 
            CurrentQty = (
                SELECT SUM(CurrentQtyBatchWise)
                FROM ItemBalance AS sub
                WHERE sub.ItemId = @ItemId
            ),
            LastUpdated = datetime('now','localtime')
        WHERE Id = (
            SELECT MAX(Id)
            FROM ItemBalance
            WHERE ItemId = @ItemId
        );
        ";

                using (var cmd = new SQLiteCommand(sqlTotal, conn, txn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.ExecuteNonQuery();
                }

                // ✅ If both SQL operations succeed
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in UpdateItemBalance: " + ex.Message);
                return false;
            }
        }
        public bool UpdateItemBalanceSales(ItemLedger entry, SQLiteConnection conn, SQLiteTransaction txn)
        {
            try
            {
                // 1️⃣ Insert or update batch-wise balance
                string sql = @"
        INSERT INTO ItemBalance (ItemId, BatchNo, CurrentQtyBatchWise, CurrentQty)
        VALUES (@ItemId, @BatchNo, @Qty, @Qty)
        ON CONFLICT(ItemId, BatchNo)
        DO UPDATE SET 
            CurrentQtyBatchWise = CurrentQtyBatchWise - excluded.CurrentQtyBatchWise,
            LastUpdated = datetime('now','localtime');
        ";

                using (var cmd = new SQLiteCommand(sql, conn, txn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.Parameters.AddWithValue("@BatchNo", entry.BatchNo ?? "");
                    cmd.Parameters.AddWithValue("@Qty", entry.Qty);
                    cmd.ExecuteNonQuery();
                }

                // 2️⃣ Update total (CurrentQty) only for the latest record
                string sqlTotal = @"
        UPDATE ItemBalance
        SET 
            CurrentQty = (
                SELECT SUM(CurrentQtyBatchWise)
                FROM ItemBalance AS sub
                WHERE sub.ItemId = @ItemId
            ),
            LastUpdated = datetime('now','localtime')
        WHERE Id = (
            SELECT MAX(Id)
            FROM ItemBalance
            WHERE ItemId = @ItemId
        );
        ";

                using (var cmd = new SQLiteCommand(sqlTotal, conn, txn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.ExecuteNonQuery();
                }

                // ✅ If both SQL operations succeed
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in UpdateItemBalance: " + ex.Message);
                return false;
            }
        }
        public bool DecreaseItemBalanceBatchWise(ItemLedger entry, SQLiteConnection conn, SQLiteTransaction txn)
        {
            try
            {

                string sqlTotal = @"
         update itembalance set currentqtybatchwise=currentqtybatchwise-@Qty where itemid=@ItemId and batchno=@BatchNo;
        ";

                using (var cmd = new SQLiteCommand(sqlTotal, conn, txn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.Parameters.AddWithValue("@BatchNo", entry.BatchNo);
                    cmd.Parameters.AddWithValue("@Qty", entry.Qty);
                    cmd.ExecuteNonQuery();
                }

                // ✅ If both SQL operations succeed
                return true;
            }
            catch (Exception ex)
            {
                txn.Rollback();
                throw;
                //Console.WriteLine("❌ Error in UpdateItemBalance: " + ex.Message);
                //return false;
            }
        }

        public bool UpdateItemBalance_ForChangeInQuantity(
    SQLiteConnection conn,
    SQLiteTransaction tran,
    string itemId,
    string batchNo,
    string invbatchno,
    string quantity)
        {
            try
            {
                // 1️⃣ Update quantity for specific batch
                string query = @"
            UPDATE ItemBalance
            SET 
                CurrentQtyBatchWise = @Qty,
                LastUpdated = datetime('now','localtime')
            WHERE ItemId = @ItemId 
              AND BatchNo = @BatchNo;
        ";

                using (var cmd1 = new SQLiteCommand(query, conn, tran))
                {
                    cmd1.Parameters.AddWithValue("@Qty", quantity);
                    cmd1.Parameters.AddWithValue("@ItemId", itemId);
                    cmd1.Parameters.AddWithValue("@BatchNo", batchNo);

                    cmd1.ExecuteNonQuery();
                }

                // 2️⃣ Update total quantity (summed across all batches)
                string sqlTotal = @"
            UPDATE ItemBalance
            SET 
                CurrentQty = (
                    SELECT SUM(CurrentQtyBatchWise)
                    FROM ItemBalance AS sub
                    WHERE sub.ItemId = @ItemId
                ),
                LastUpdated = datetime('now','localtime')
            WHERE Id = (
                SELECT MAX(Id)
                FROM ItemBalance
                WHERE ItemId = @ItemId
            );
        ";

                using (var cmd2 = new SQLiteCommand(sqlTotal, conn, tran))
                {
                    cmd2.Parameters.AddWithValue("@ItemId", itemId);
                    cmd2.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating ItemBalance quantities: " + ex.Message);
                return false;
            }
        }






        public bool AddItemLedger(ItemLedger entry, SQLiteConnection conn, SQLiteTransaction txn)
        {
            //using (var conn = new SQLiteConnection(_connectionString))
            //{
            //conn.Open();
            try
            {
                string sql = @"
            INSERT INTO ItemLedger 
            (ItemId, BatchNo, Date, TxnType, RefNo, Qty, Rate, DiscountPercent, NetRate, TotalAmount, Remarks, CreatedBy)
            VALUES 
            (@ItemId, @BatchNo, @Date, @TxnType, @RefNo, @Qty, @Rate, @DiscountPercent, @NetRate ,@Amount, @Remarks, @CreatedBy);
        ";

                using (var cmd = new SQLiteCommand(sql, conn, txn))
                {

                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.Parameters.AddWithValue("@BatchNo", entry.BatchNo ?? "");
                    cmd.Parameters.AddWithValue("@Date", entry.Date);
                    cmd.Parameters.AddWithValue("@TxnType", entry.TxnType);
                    cmd.Parameters.AddWithValue("@RefNo", entry.RefNo ?? "");
                    cmd.Parameters.AddWithValue("@Qty", entry.Qty);
                    cmd.Parameters.AddWithValue("@Rate", entry.Rate);
                    cmd.Parameters.AddWithValue("@DiscountPercent", entry.DiscountPercent);
                    cmd.Parameters.AddWithValue("@NetRate", entry.NetRate);
                    cmd.Parameters.AddWithValue("@Amount", entry.TotalAmount);
                    cmd.Parameters.AddWithValue("@Remarks", entry.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@CreatedBy", entry.CreatedBy ?? "System");

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ AddItemDetails failed: " + ex.Message);
                return false;
            }

            // }
        }
        public ItemBalance GetItemBalance(int itemId, string batchNo = null)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = batchNo == null
                    ? "SELECT * FROM ItemBalance WHERE ItemId = @ItemId LIMIT 1;"
                    : "SELECT * FROM ItemBalance WHERE ItemId = @ItemId AND BatchNo = @BatchNo LIMIT 1;";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", itemId);
                    if (batchNo != null)
                        cmd.Parameters.AddWithValue("@BatchNo", batchNo);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ItemBalance
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ItemId = Convert.ToInt32(reader["ItemId"]),
                                BatchNo = reader["BatchNo"].ToString(),
                                CurrentQty = Convert.ToDouble(reader["CurrentQty"]),
                                CurrentQtyBatchWise = Convert.ToDouble(reader["CurrentQtyBatchWise"]),
                                LastUpdated = reader["LastUpdated"].ToString()
                            };
                        }
                    }
                }

                return null;
            }
        }
        public CompanyProfile GetCompanyProfile()
        {
            CompanyProfile profile = null;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM CompanyProfile LIMIT 1";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        profile = new CompanyProfile
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            CompanyName = reader["CompanyName"]?.ToString(),
                            AddressLine1 = reader["AddressLine1"]?.ToString(),
                            AddressLine2 = reader["AddressLine2"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            State = reader["State"]?.ToString(),
                            Pincode = reader["Pincode"]?.ToString(),
                            Country = reader["Country"]?.ToString(),

                            GSTIN = reader["GSTIN"]?.ToString(),
                            PAN = reader["PAN"]?.ToString(),

                            Email = reader["Email"]?.ToString(),
                            Phone = reader["Phone"]?.ToString(),

                            BankName = reader["BankName"]?.ToString(),
                            BankAccount = reader["BankAccount"]?.ToString(),
                            IFSC = reader["IFSC"]?.ToString(),
                            BranchName = reader["BranchName"]?.ToString(),

                            InvoicePrefix = reader["InvoicePrefix"]?.ToString(),
                            InvoiceStartNo = Convert.ToInt32(reader["InvoiceStartNo"]),
                            CurrentInvoiceNo = Convert.ToInt32(reader["CurrentInvoiceNo"]),

                            Logo = reader["Logo"] as byte[],

                            CreatedBy = reader["CreatedBy"]?.ToString(),
                            CreatedAt = reader["CreatedAt"]?.ToString()
                        };
                    }
                }
            }

            return profile;
        }
        public CompanyProfileSRDto GetCompanyProfileSR()
        {
            CompanyProfileSRDto profile = null;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM CompanyProfile LIMIT 1";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        profile = new CompanyProfileSRDto
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            CompanyName = reader["CompanyName"]?.ToString(),
                            AddressLine1 = reader["AddressLine1"]?.ToString(),
                            AddressLine2 = reader["AddressLine2"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            State = reader["State"]?.ToString(),
                            Pincode = reader["Pincode"]?.ToString(),
                            Country = reader["Country"]?.ToString(),

                            GSTIN = reader["GSTIN"]?.ToString(),
                            PAN = reader["PAN"]?.ToString(),

                            Email = reader["Email"]?.ToString(),
                            Phone = reader["Phone"]?.ToString(),

                            BankName = reader["BankName"]?.ToString(),
                            BankAccount = reader["BankAccount"]?.ToString(),
                            IFSC = reader["IFSC"]?.ToString(),
                            BranchName = reader["BranchName"]?.ToString(),

                            InvoicePrefix = reader["InvoicePrefix"]?.ToString(),
                            InvoiceStartNo = Convert.ToInt32(reader["InvoiceStartNo"]),
                            CurrentInvoiceNo = Convert.ToInt32(reader["CurrentInvoiceNo"]),

                            Logo = reader["Logo"] as byte[],

                            CreatedBy = reader["CreatedBy"]?.ToString(),
                            CreatedAt = reader["CreatedAt"]?.ToString()
                        };
                    }
                }
            }

            return profile;
        }
        public bool SaveCompanyProfile(CompanyProfile data)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // Check if record exists
                string countQuery = "SELECT COUNT(*) FROM CompanyProfile";
                int count = Convert.ToInt32(new SQLiteCommand(countQuery, conn).ExecuteScalar());

                if (count == 0)
                {
                    // INSERT
                    string insertQuery = @"
                INSERT INTO CompanyProfile 
                (CompanyName, AddressLine1, AddressLine2, City, State, Pincode, Country,
                 GSTIN, PAN, Email, Phone, BankName, BankAccount, IFSC, BranchName, 
                 InvoicePrefix, InvoiceStartNo, CurrentInvoiceNo, Logo, CreatedBy, CreatedAt)
                VALUES 
                (@CompanyName, @AddressLine1, @AddressLine2, @City, @State, @Pincode, @Country,
                 @GSTIN, @PAN, @Email, @Phone, @BankName, @BankAccount, @IFSC, @BranchName,
                 @InvoicePrefix, @InvoiceStartNo, @CurrentInvoiceNo, @Logo, @CreatedBy, datetime('now', 'localtime'));
            ";

                    using (var cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        BindCompanyParams(cmd, data);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                else
                {
                    // UPDATE
                    string updateQuery = @"
                UPDATE CompanyProfile SET
                    CompanyName=@CompanyName,
                    AddressLine1=@AddressLine1, AddressLine2=@AddressLine2,
                    City=@City, State=@State, Pincode=@Pincode, Country=@Country,
                    GSTIN=@GSTIN, PAN=@PAN, Email=@Email, Phone=@Phone,
                    BankName=@BankName, BankAccount=@BankAccount, IFSC=@IFSC, BranchName=@BranchName,
                    InvoicePrefix=@InvoicePrefix, InvoiceStartNo=@InvoiceStartNo, CurrentInvoiceNo=@CurrentInvoiceNo,
                    Logo=@Logo, CreatedBy=@CreatedBy
                WHERE Id = @Id;
            ";

                    using (var cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        BindCompanyParams(cmd, data);
                        cmd.Parameters.AddWithValue("@Id", data.Id);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
        }
        public void BindCompanyParams(SQLiteCommand cmd, CompanyProfile data)
        {
            cmd.Parameters.AddWithValue("@CompanyName", data.CompanyName);
            cmd.Parameters.AddWithValue("@AddressLine1", data.AddressLine1);
            cmd.Parameters.AddWithValue("@AddressLine2", data.AddressLine2);
            cmd.Parameters.AddWithValue("@City", data.City);
            cmd.Parameters.AddWithValue("@State", data.State);
            cmd.Parameters.AddWithValue("@Pincode", data.Pincode);
            cmd.Parameters.AddWithValue("@Country", data.Country);

            cmd.Parameters.AddWithValue("@GSTIN", data.GSTIN);
            cmd.Parameters.AddWithValue("@PAN", data.PAN);

            cmd.Parameters.AddWithValue("@Email", data.Email);
            cmd.Parameters.AddWithValue("@Phone", data.Phone);

            cmd.Parameters.AddWithValue("@BankName", data.BankName);
            cmd.Parameters.AddWithValue("@BankAccount", data.BankAccount);
            cmd.Parameters.AddWithValue("@IFSC", data.IFSC);
            cmd.Parameters.AddWithValue("@BranchName", data.BranchName);

            cmd.Parameters.AddWithValue("@InvoicePrefix", data.InvoicePrefix);
            cmd.Parameters.AddWithValue("@InvoiceStartNo", data.InvoiceStartNo);
            cmd.Parameters.AddWithValue("@CurrentInvoiceNo", data.CurrentInvoiceNo);

            cmd.Parameters.AddWithValue("@Logo", data.Logo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", data.CreatedBy);
        }
        public void IncrementInvoiceNo()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = "UPDATE CompanyProfile SET CurrentInvoiceNo = CurrentInvoiceNo + 1";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }
        // Helper: convert base64 string -> byte[]
        //private byte[] Base64ToBytes(string base64)
        //{
        //    if (string.IsNullOrEmpty(base64)) return null;
        //    try { return Convert.FromBase64String(base64); }
        //    catch { return null; }
        //}
        public (string InvoiceNo, int InvoiceNum) GetNextInvoiceNumber(int companyProfileId = 1)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var txn = conn.BeginTransaction())
                {
                    // read current
                    string q = "SELECT InvoicePrefix, CurrentInvoiceNo FROM CompanyProfile WHERE Id = @Id LIMIT 1";
                    using (var cmd = new SQLiteCommand(q, conn, txn))
                    {
                        cmd.Parameters.AddWithValue("@Id", companyProfileId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) throw new Exception("CompanyProfile missing");
                            string prefix = (r["InvoicePrefix"]?.ToString()) ?? "INV-";
                            int cur = r["CurrentInvoiceNo"] == DBNull.Value ? 1 : Convert.ToInt32(r["CurrentInvoiceNo"]);
                            int next = cur;

                            // format with leading zeros (customize digits)
                            string formatted = $"{prefix}{next:00000}";

                            // increment and save
                            string upd = "UPDATE CompanyProfile SET CurrentInvoiceNo = @Next WHERE Id = @Id";
                            using (var cmd2 = new SQLiteCommand(upd, conn, txn))
                            {
                                cmd2.Parameters.AddWithValue("@Next", next + 1);
                                cmd2.Parameters.AddWithValue("@Id", companyProfileId);
                                cmd2.ExecuteNonQuery();
                            }

                            txn.Commit();
                            return (formatted, next);
                        }
                    }
                }
            }
        }
        public bool DecreaseItemBalance(ItemLedger entry, SQLiteConnection conn, SQLiteTransaction txn)
        {
            try
            {

                string sqlTotal = @"
        update itembalance set currentqty=currentqty-@Qty where itemid=@ItemId and id=(
            SELECT MAX(Id)
            FROM ItemBalance
            WHERE ItemId = @ItemId
        );
        ";

                using (var cmd = new SQLiteCommand(sqlTotal, conn, txn))
                {
                    cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
                    cmd.Parameters.AddWithValue("@Qty", entry.Qty);
                    cmd.ExecuteNonQuery();
                }

                // ✅ If both SQL operations succeed
                return true;
            }
            catch (Exception ex)
            {
                txn.Rollback();
                throw;
                //Console.WriteLine("❌ Error in UpdateItemBalance: " + ex.Message);
                //return false;
            }
        }
        public (long invoiceId, string invoiceNo) CreateInvoice(CreateInvoiceDto dto)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        //-------------------------------------
                        // 1️⃣ Insert or Update Customer (Merged)
                        //-------------------------------------

                        int customerId = 0;

                        if (dto.Customer != null)
                        {
                            var cust = dto.Customer;

                            // 🔍 Check existing by phone
                            if (!string.IsNullOrWhiteSpace(cust.Phone))
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tx;
                                    cmd.CommandText = "SELECT Id FROM Customers WHERE Phone = @phone LIMIT 1;";
                                    cmd.Parameters.AddWithValue("@phone", cust.Phone);

                                    var existingId = cmd.ExecuteScalar();
                                    if (existingId != null)
                                    {
                                        customerId = Convert.ToInt32(existingId);
                                    }
                                }
                            }

                            // ➕ If not found → insert new customer
                            if (customerId == 0)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tx;
                                    cmd.CommandText = @"
                                INSERT INTO Customers (Name, Phone, State, Address)
                                VALUES (@Name, @Phone, @State, @Address);

                                SELECT last_insert_rowid();
                            ";

                                    cmd.Parameters.AddWithValue("@Name", cust.Name ?? "");
                                    cmd.Parameters.AddWithValue("@Phone", cust.Phone ?? "");
                                    cmd.Parameters.AddWithValue("@State", cust.State ?? "");
                                    cmd.Parameters.AddWithValue("@Address", cust.Address ?? "");

                                    customerId = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                            }

                            // assign back
                            dto.Customer.Id = customerId;
                        }

                        //-------------------------------------
                        // 2️⃣ Generate Invoice Number (Merged)
                        //-------------------------------------

                        string prefix = "";
                        long startNo = 1;
                        long currentNo = 1;

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
                        SELECT InvoicePrefix, InvoiceStartNo, CurrentInvoiceNo 
                        FROM CompanyProfile ORDER BY Id LIMIT 1";

                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read())
                                    throw new Exception("Company profile not found");

                                prefix = r.IsDBNull(0) ? "" : r.GetString(0);
                                startNo = r.IsDBNull(1) ? 1 : r.GetInt64(1);
                                currentNo = r.IsDBNull(2) ? startNo : r.GetInt64(2);
                            }
                        }

                        int nextNo = (int)(currentNo + 1);
                        string fullInvoiceNo = prefix + nextNo.ToString();

                        //-------------------------------------
                        // 3️⃣ Insert Invoice Header
                        //-------------------------------------


                        long invoiceId;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;

                            cmd.CommandText = @"
                        INSERT INTO Invoice (
                            InvoiceNo, InvoiceNum,
                            InvoiceDate, CompanyProfileId,
                            CustomerId, CustomerName, CustomerPhone, CustomerState, CustomerAddress,
                            SubTotal, TotalTax, TotalAmount, RoundOff,
                            CreatedBy
                        )
                        VALUES (
                            @InvoiceNo, @InvoiceNum,
                            @InvoiceDate, @CompanyProfileId,
                            @CustomerId, @CustomerName, @CustomerPhone, @CustomerState, @CustomerAddress,
                            @SubTotal, @TotalTax, @TotalAmount, @RoundOff,
                            @CreatedBy
                        );

                        SELECT last_insert_rowid();
                    ";

                            cmd.Parameters.AddWithValue("@InvoiceNo", fullInvoiceNo);
                            cmd.Parameters.AddWithValue("@InvoiceNum", nextNo);

                            cmd.Parameters.AddWithValue("@InvoiceDate", dto.InvoiceDate);
                            cmd.Parameters.AddWithValue("@CompanyProfileId", dto.CompanyId);

                            cmd.Parameters.AddWithValue("@CustomerId", dto.Customer.Id);
                            cmd.Parameters.AddWithValue("@CustomerName", dto.Customer.Name);
                            cmd.Parameters.AddWithValue("@CustomerPhone", dto.Customer.Phone);
                            cmd.Parameters.AddWithValue("@CustomerState", dto.Customer.State);
                            cmd.Parameters.AddWithValue("@CustomerAddress", dto.Customer.Address);

                            cmd.Parameters.AddWithValue("@SubTotal", dto.SubTotal);
                            cmd.Parameters.AddWithValue("@TotalTax", dto.TotalTax);
                            cmd.Parameters.AddWithValue("@TotalAmount", dto.TotalAmount);
                            cmd.Parameters.AddWithValue("@RoundOff", dto.RoundOff);

                            cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);
                            //cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            invoiceId = (long)cmd.ExecuteScalar();
                        }

                        //-------------------------------------
                        // 4️⃣ Insert Invoice Items
                        //-------------------------------------

                        foreach (var item in dto.Items)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;

                                cmd.CommandText = @"
                            INSERT INTO InvoiceItems (
                                InvoiceId,
                                ItemId, BatchNo, HsnCode,
                                Qty, Rate, DiscountPercent,
                                GstPercent, GstValue,
                                CgstPercent, CgstValue,
                                SgstPercent, SgstValue,
                                IgstPercent, IgstValue,
                                LineSubTotal, LineTotal
                            )
                            VALUES (
                                @InvoiceId,
                                @ItemId, @BatchNo, @HsnCode,
                                @Qty, @Rate, @DiscountPercent,
                                @GstPercent, @GstValue,
                                @CgstPercent, @CgstValue,
                                @SgstPercent, @SgstValue,
                                @IgstPercent, @IgstValue,
                                @LineSubTotal, @LineTotal
                            );
                        ";

                                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                                cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                                cmd.Parameters.AddWithValue("@BatchNo", item.BatchNo);
                                cmd.Parameters.AddWithValue("@HsnCode", item.HsnCode);

                                cmd.Parameters.AddWithValue("@Qty", item.Qty);
                                cmd.Parameters.AddWithValue("@Rate", item.Rate);
                                cmd.Parameters.AddWithValue("@DiscountPercent", item.DiscountPercent);

                                cmd.Parameters.AddWithValue("@GstPercent", item.GstPercent);
                                cmd.Parameters.AddWithValue("@GstValue", item.GstValue);

                                cmd.Parameters.AddWithValue("@CgstPercent", item.CgstPercent);
                                cmd.Parameters.AddWithValue("@CgstValue", item.CgstValue);

                                cmd.Parameters.AddWithValue("@SgstPercent", item.SgstPercent);
                                cmd.Parameters.AddWithValue("@SgstValue", item.SgstValue);

                                cmd.Parameters.AddWithValue("@IgstPercent", item.IgstPercent);
                                cmd.Parameters.AddWithValue("@IgstValue", item.IgstValue);

                                cmd.Parameters.AddWithValue("@LineSubTotal", item.LineSubTotal);
                                cmd.Parameters.AddWithValue("@LineTotal", item.LineTotal);

                                cmd.ExecuteNonQuery();
                            }
                            decimal discountPercent = item.DiscountPercent;
                            decimal netRate = item.Rate - (item.Rate * discountPercent / 100);
                            decimal lineTotal = item.LineTotal;

                            ItemLedger ledgerEntry = new ItemLedger();
                            ledgerEntry.ItemId = item.ItemId;
                            ledgerEntry.BatchNo = item.BatchNo;
                            ledgerEntry.Date = dto.InvoiceDate;
                            ledgerEntry.TxnType = "SALE";
                            ledgerEntry.RefNo = fullInvoiceNo;
                            ledgerEntry.Qty = item.Qty;
                            ledgerEntry.Rate = item.Rate;
                            ledgerEntry.DiscountPercent = discountPercent;
                            ledgerEntry.NetRate = netRate;
                            ledgerEntry.TotalAmount = lineTotal;
                            ledgerEntry.Remarks = "Invoice Sale";
                            ledgerEntry.CreatedBy = dto.CreatedBy;

                            AddItemLedger(ledgerEntry, conn, tx);


                            //UpdateItemBalance(ledgerEntry, conn, tx);
                            // Update balances
                            DecreaseItemBalance(ledgerEntry, conn, tx);
                            DecreaseItemBalanceBatchWise(ledgerEntry, conn, tx);
                        }

                        //-------------------------------------
                        // 5️⃣ Update CurrentInvoiceNo
                        //-------------------------------------

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "UPDATE CompanyProfile SET CurrentInvoiceNo = @n";
                            cmd.Parameters.AddWithValue("@n", nextNo);
                            cmd.ExecuteNonQuery();
                        }

                        //-------------------------------------
                        // 6️⃣ Commit and return both values
                        //-------------------------------------

                        tx.Commit();
                        return (invoiceId, fullInvoiceNo);
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }


        public Models.InvoiceLoadDto GetInvoice(long invoiceId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                var dto = new Models.InvoiceLoadDto();
                dto.Items = new List<Models.InvoiceItemDto>();

                // 1️⃣ Load invoice header
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT 
                    Id,
                    InvoiceNo, InvoiceNum,
                    InvoiceDate,
                    CompanyProfileId,

                    CustomerId, CustomerName, CustomerPhone, CustomerState, CustomerAddress,

                    SubTotal, TotalTax, TotalAmount, RoundOff
                FROM Invoice
                WHERE Id = @Id
            ";
                    cmd.Parameters.AddWithValue("@Id", invoiceId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            return null;  // invoice not found
                        }

                        dto.Id = r.GetInt64(0);
                        dto.InvoiceNo = r.GetString(1);
                        dto.InvoiceNum = r.GetInt32(2);
                        dto.InvoiceDate = r.GetString(3);
                        dto.CompanyProfileId = r.GetInt32(4);

                        dto.CustomerId = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                        dto.CustomerName = r.IsDBNull(6) ? "" : r.GetString(6);
                        dto.CustomerPhone = r.IsDBNull(7) ? "" : r.GetString(7);
                        dto.CustomerState = r.IsDBNull(8) ? "" : r.GetString(8);
                        dto.CustomerAddress = r.IsDBNull(9) ? "" : r.GetString(9);

                        dto.SubTotal = r.GetDecimal(10);
                        dto.TotalTax = r.GetDecimal(11);
                        dto.TotalAmount = r.GetDecimal(12);
                        dto.RoundOff = r.GetDecimal(13);
                    }
                }

                // 2️⃣ Load invoice items
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT
                    ItemId, BatchNo, HsnCode,
                    Qty, Rate, DiscountPercent,
                    GstPercent, GstValue,
                    CgstPercent, CgstValue,
                    SgstPercent, SgstValue,
                    IgstPercent, IgstValue,
                    LineSubTotal, LineTotal
                FROM InvoiceItems
                WHERE InvoiceId = @Id
            ";
                    cmd.Parameters.AddWithValue("@Id", invoiceId);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var item = new Models.InvoiceItemDto
                            {
                                ItemId = r.GetInt32(0),
                                BatchNo = r.IsDBNull(1) ? "" : r.GetString(1),
                                HsnCode = r.IsDBNull(2) ? "" : r.GetString(2),

                                Qty = r.GetDecimal(3),
                                Rate = r.GetDecimal(4),
                                DiscountPercent = r.GetDecimal(5),

                                GstPercent = r.GetDecimal(6),
                                GstValue = r.GetDecimal(7),

                                CgstPercent = r.GetDecimal(8),
                                CgstValue = r.GetDecimal(9),

                                SgstPercent = r.GetDecimal(10),
                                SgstValue = r.GetDecimal(11),

                                IgstPercent = r.GetDecimal(12),
                                IgstValue = r.GetDecimal(13),

                                LineSubTotal = r.GetDecimal(14),
                                LineTotal = r.GetDecimal(15)
                            };

                            dto.Items.Add(item);
                        }
                    }
                }

                return dto;
            }
        }

        public Models.InvoiceLoadDto GetInvoiceForReturn(long invoiceId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                var dto = new Models.InvoiceLoadDto();
                dto.Items = new List<Models.InvoiceItemDto>();

                // 1️⃣ Load invoice header
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT 
                    Id,
                    InvoiceNo, InvoiceNum,
                    InvoiceDate,
                    CompanyProfileId,

                    CustomerId, CustomerName, CustomerPhone, CustomerState, CustomerAddress,

                    SubTotal, TotalTax, TotalAmount, RoundOff
                FROM Invoice
                WHERE Id = @Id
            ";
                    cmd.Parameters.AddWithValue("@Id", invoiceId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            return null;  // invoice not found
                        }

                        dto.Id = r.GetInt64(0);
                        dto.InvoiceNo = r.GetString(1);
                        dto.InvoiceNum = r.GetInt32(2);
                        dto.InvoiceDate = r.GetString(3);
                        dto.CompanyProfileId = r.GetInt32(4);

                        dto.CustomerId = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                        dto.CustomerName = r.IsDBNull(6) ? "" : r.GetString(6);
                        dto.CustomerPhone = r.IsDBNull(7) ? "" : r.GetString(7);
                        dto.CustomerState = r.IsDBNull(8) ? "" : r.GetString(8);
                        dto.CustomerAddress = r.IsDBNull(9) ? "" : r.GetString(9);

                        dto.SubTotal = r.GetDecimal(10);
                        dto.TotalTax = r.GetDecimal(11);
                        dto.TotalAmount = r.GetDecimal(12);
                        dto.RoundOff = r.GetDecimal(13);
                    }
                }

                // 2️⃣ Load invoice items
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT
    ItemId,item.name BatchNo, HsnCode,
    Qty, Rate, DiscountPercent,
    GstPercent, GstValue,
    CgstPercent, CgstValue,
    SgstPercent, SgstValue,
    IgstPercent, IgstValue,
    LineSubTotal, LineTotal
FROM InvoiceItems
inner join item on item.id=invoiceitems.itemid
WHERE InvoiceId =@Id
            ";
                    cmd.Parameters.AddWithValue("@Id", invoiceId);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var item = new Models.InvoiceItemDto
                            {
                                ItemId = r.GetInt32(0),
                                BatchNo = r.IsDBNull(1) ? "" : r.GetString(1),
                                ItemName = r.IsDBNull(2) ? "" : r.GetString(2),
                                HsnCode = r.IsDBNull(3) ? "" : r.GetString(3),

                                Qty = r.GetDecimal(4),
                                Rate = r.GetDecimal(5),
                                DiscountPercent = r.GetDecimal(6),

                                GstPercent = r.GetDecimal(7),
                                GstValue = r.GetDecimal(8),

                                CgstPercent = r.GetDecimal(9),
                                CgstValue = r.GetDecimal(10),

                                SgstPercent = r.GetDecimal(11),
                                SgstValue = r.GetDecimal(12),

                                IgstPercent = r.GetDecimal(13),
                                IgstValue = r.GetDecimal(14),

                                LineSubTotal = r.GetDecimal(15),
                                LineTotal = r.GetDecimal(16)
                            };

                            dto.Items.Add(item);
                        }
                    }
                }

                return dto;
            }
        }
        public int GetNextSalesReturnNumber(SQLiteConnection conn, IDbTransaction tran)
        {
            // Fetch greatest number; if table is empty return 1
            const string sql = @"SELECT IFNULL(MAX(ReturnNum), 0) FROM SalesReturn;";

            int lastNum = conn.ExecuteScalar<int>(sql, transaction: tran);
            return lastNum + 1;
        }

        public (bool Success, int ReturnId) SaveSalesReturn(SalesReturnDto dto)
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // Generate running number
                    int nextNum = GetNextSalesReturnNumber(conn, tran);
                    dto.ReturnNum = nextNum;
                    dto.ReturnNo = $"SR-{nextNum:D4}";

                    // Insert header
                    string insertHeader = @"
            INSERT INTO SalesReturn
            (ReturnNo, ReturnNum, ReturnDate, InvoiceId, InvoiceNo, CustomerId,
             SubTotal, TotalTax, TotalAmount, RoundOff, Notes, CreatedBy, CreatedAt)
            VALUES
            (@ReturnNo, @ReturnNum, @ReturnDate, @InvoiceId, @InvoiceNo, @CustomerId,
             @SubTotal, @TotalTax, @TotalAmount, @RoundOff, @Notes, @CreatedBy, @CreatedAt);
            SELECT last_insert_rowid();
        ";

                    int salesReturnId = conn.ExecuteScalar<int>(
                        insertHeader,
                        new
                        {
                            dto.ReturnNo,
                            dto.ReturnNum,
                            ReturnDate = dto.ReturnDate.ToString("yyyy-MM-dd"),
                            dto.InvoiceId,
                            dto.InvoiceNo,
                            dto.CustomerId,
                            dto.SubTotal,
                            dto.TotalTax,
                            dto.TotalAmount,
                            dto.RoundOff,
                            dto.Notes,
                            dto.CreatedBy,
                            CreatedAt = dto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                        },
                        transaction: tran
                    );

                    // Insert items
                    string insertLine = @"
            INSERT INTO SalesReturnItem
            (SalesReturnId, InvoiceItemId, ItemId, BatchNo, Qty, Rate,
             DiscountPercent,
             GstPercent, GstValue, CgstPercent, CgstValue,
             SgstPercent, SgstValue, IgstPercent, IgstValue,
             LineSubTotal, LineTotal)
            VALUES
            (@SalesReturnId, @InvoiceItemId, @ItemId, @BatchNo, @Qty, @Rate,
             @DiscountPercent,
             @GstPercent, @GstValue, @CgstPercent, @CgstValue,
             @SgstPercent, @SgstValue, @IgstPercent, @IgstValue,
             @LineSubTotal, @LineTotal);
        ";

                    foreach (var item in dto.Items)
                    {
                        if (item.Qty <= 0) continue;

                        conn.Execute(
                            insertLine,
                            new
                            {
                                SalesReturnId = salesReturnId,
                                item.InvoiceItemId,
                                item.ItemId,
                                item.BatchNo,
                                item.Qty,
                                item.Rate,
                                item.DiscountPercent,
                                item.GstPercent,
                                item.GstValue,
                                item.CgstPercent,
                                item.CgstValue,
                                item.SgstPercent,
                                item.SgstValue,
                                item.IgstPercent,
                                item.IgstValue,
                                item.LineSubTotal,
                                item.LineTotal
                            },
                            transaction: tran
                        );

                        // Increase ReturnedQty in InvoiceItems
                        string updateReturned = @"
                UPDATE InvoiceItems
                SET ReturnedQty = ReturnedQty + @Qty
                WHERE Id = @InvoiceItemId;
            ";
                        conn.Execute(updateReturned, new { item.Qty, item.InvoiceItemId }, transaction: tran);

                        // Increment batch stock
                        //UpdateBatchStock(item.ItemId, item.BatchNo, +item.Qty, tran);

                        ItemLedger ledgerEntry = new ItemLedger();
                        ledgerEntry.ItemId = item.ItemId;
                        ledgerEntry.BatchNo = item.BatchNo;
                        ledgerEntry.Date = dto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                        ledgerEntry.TxnType = "SALES RETURN";
                        ledgerEntry.RefNo = dto.ReturnNo;
                        ledgerEntry.Qty = item.Qty;
                        ledgerEntry.Rate = item.Rate;
                        ledgerEntry.DiscountPercent = item.DiscountPercent;
                        decimal netRate = item.Rate - (item.Rate * item.DiscountPercent / 100);
                        ledgerEntry.NetRate = netRate;
                        ledgerEntry.TotalAmount = item.LineTotal;
                        ledgerEntry.Remarks = "Sales Return";
                        ledgerEntry.CreatedBy = dto.CreatedBy;
                        AddItemLedger(ledgerEntry, conn, tran);
                        UpdateItemBalance(ledgerEntry, conn, tran);

                    }

                    tran.Commit();
                    return (true, salesReturnId);
                }
                catch
                {
                    tran.Rollback();
                    return (false, 0);
                    //throw;
                }
            }
        }

        public SalesReturnDto LoadSalesReturnDetail(int id)
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            var header = conn.QuerySingle<SalesReturnDto>(
                "SELECT * FROM SalesReturn WHERE Id=@id", new { id });

            var items = conn.Query<SalesReturnItemDto>(@"
            SELECT SalesReturnItem.*,item.name as ItemName 
FROM SalesReturnItem
inner join item on item.id=SalesReturnItem.itemid
WHERE SalesReturnId=@id",
                new { id }).ToList();

            header.Items = items;
            return header;
        }


        public List<CustomerDto> GetCustomers()
        {
            var list = new List<CustomerDto>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
            SELECT 
                Id,
                Name,
                Phone,
State,
                Address
            FROM Customers
            ORDER BY Name ASC;
        ";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CustomerDto
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            State = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Address = reader.IsDBNull(3) ? "" : reader.GetString(3)
                        });
                    }
                }
            }

            return list;
        }
        public int InsertOrUpdateCustomer(CustomerDto c)
        {
            if (c == null) return 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // 1. If phone exists, update + return ID
                if (!string.IsNullOrWhiteSpace(c.Phone))
                {
                    string sqlFind = "SELECT Id FROM Customers WHERE Phone = @phone LIMIT 1;";
                    using (var cmd = new SQLiteCommand(sqlFind, conn))
                    {
                        cmd.Parameters.AddWithValue("@phone", c.Phone);
                        var existingId = cmd.ExecuteScalar();
                        if (existingId != null)
                        {
                            return Convert.ToInt32(existingId);
                        }
                    }
                }

                // 2. Insert new customer
                string sqlInsert = @"
INSERT INTO Customers (Name, Phone, State, Address)
VALUES (@Name, @Phone, @State, @Address);";

                using (var cmd = new SQLiteCommand(sqlInsert, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", c.Name ?? "");
                    cmd.Parameters.AddWithValue("@Phone", c.Phone ?? "");
                    cmd.Parameters.AddWithValue("@State", c.State ?? "");
                    cmd.Parameters.AddWithValue("@Address", c.Address ?? "");
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        public (string fullNo, int nextNo) GenerateNextInvoiceNo()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
        SELECT InvoicePrefix, InvoiceStartNo, CurrentInvoiceNo
        FROM CompanyProfile ORDER BY Id LIMIT 1";

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new Exception("Company profile not found");

                        string prefix = r.IsDBNull(0) ? "" : r.GetString(0);
                        long startNo = r.IsDBNull(1) ? 1 : r.GetInt64(1);
                        long currentNo = r.IsDBNull(2) ? startNo : r.GetInt64(2);

                        long next = currentNo + 1;

                        string fullInvoice = $"{prefix}{next}";

                        return (fullInvoice, (int)next);
                    }
                }
            }

        }
        public void UpdateCurrentInvoiceNo(int newNo)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE CompanyProfile SET CurrentInvoiceNo = @no";
                    cmd.Parameters.AddWithValue("@no", newNo);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public int GetItemBalance(int itemId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("select currentqty from itembalance\r\nwhere itemid=@itemid and id=(select max(id) from itembalance where itemid=@itemid)", conn);
                cmd.Parameters.AddWithValue("@itemid", itemId);
                var r = cmd.ExecuteScalar();
                return r != null ? Convert.ToInt32(r) : 0;
            }
        }
        public int GetItemBalanceBatchWise(int itemId, string batchno)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("select currentqtybatchwise from itembalance\r\nwhere itemid=@itemid and batchno=@batchno", conn);
                cmd.Parameters.AddWithValue("@itemid", itemId);
                cmd.Parameters.AddWithValue("@batchno", batchno);
                var r = cmd.ExecuteScalar();
                return r != null ? Convert.ToInt32(r) : 0;
            }
        }
        public IEnumerable<dynamic> GetInvoiceNumbersByDate(string date)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                return conn.Query(
                    "SELECT Id, InvoiceNo FROM Invoice WHERE DATE(CreatedAt) = DATE(@date) ORDER BY InvoiceNo",
                    new { date }
                ).ToList();
            }
        }
        //public List<string> ValidateInvoice(InvoiceDto invoice)
        //{
        //    var errors = new List<string>();

        //    // 🔵 1. Invoice Date - Mandatory
        //    if (invoice.InvoiceDate == null)
        //    {
        //        errors.Add("Missing invoice date.");
        //    }
        //    else
        //    {
        //        // 🔵 Convert to DateOnly / DateTime depending on your type
        //        DateTime date;

        //        string dateText = invoice.InvoiceDate?.Trim();

        //        if (string.IsNullOrWhiteSpace(dateText))
        //        {
        //            errors.Add("Invoice date is required.");
        //            return errors;
        //        }

        //        // Try parsing as yyyy-MM-dd first (best for HTML <input type="date">)
        //        if (!DateTime.TryParseExact(
        //                dateText,
        //                "yyyy-MM-dd",
        //                System.Globalization.CultureInfo.InvariantCulture,
        //                System.Globalization.DateTimeStyles.None,
        //                out date))
        //        {
        //            // Fallback to normal parsing (2025-11-21T00:00:00 OR 11/21/2025, etc.)
        //            if (!DateTime.TryParse(dateText, out date))
        //            {
        //                errors.Add("Invalid invoice date format.");
        //                return errors;
        //            }
        //        }

        //        // Now you can safely use `date`
        //        date = date.Date;


        //        // 🔵 2. Invalid Calendar Date
        //        if (date.Year < 2000 || date.Year > 2100)
        //            errors.Add("Invoice date year must be between 2000 and 2100.");

        //        // 🔵 3. Future Date Not Allowed
        //        if (date > DateTime.Today)
        //            errors.Add("Invoice date cannot be in the future.");
        //    }

        //    //if (string.IsNullOrWhiteSpace(invoice.InvoiceNo))
        //    //    errors.Add("Missing invoice number.");

        //    //if (string.IsNullOrWhiteSpace(invoice.CustomerName))
        //    //    errors.Add("Missing customer name.");

        //    if (invoice.Items == null || invoice.Items.Count == 0)
        //        errors.Add("No items found.");

        //    foreach (var item in invoice.Items)
        //    {
        //        if (item.Qty <= 0)
        //            errors.Add($"Item {item.ItemId}: Quantity must be > 0.");

        //        if (item.Rate <= 0)
        //            errors.Add($"Item {item.Rate}: Rate must be > 0.");

        //        if (item.BatchNo=="")
        //            errors.Add($"Item {item.BatchNo}: Batch can not be blank.");

        //        if (item.HsnCode == "")
        //            errors.Add($"Item {item.HsnCode}: HsnCode can not be blank.");

        //        if (item.Qty <= 0)
        //            errors.Add($"Item {item.Qty}: Qty can not be <=0.");


        //        if (item.DiscountPercent < 0 || item.DiscountPercent > 100)
        //        {
        //            errors.Add($"Item {item.GstPercent}: Discount must be >= 0.");
        //        }


        //        if (item.GstPercent < 0)
        //            errors.Add($"Item {item.GstPercent}: GstPercent must be >= 0.");


        //    }

        //    return errors;
        //}
        public List<string> ValidateItem(DhanSutra.Models.Item item)
        {
            var errors = new List<string>();

            // Item Name
            if (string.IsNullOrWhiteSpace(item.Name))
                errors.Add("Item Name cannot be empty.");

            // Item Code
            if (string.IsNullOrWhiteSpace(item.ItemCode))
                errors.Add("Item Code cannot be empty.");

            // Category
            if (item.CategoryId <= 0)
                errors.Add("Category must be selected.");

            // Date
            if (item.Date == null)
                errors.Add("Date is required.");
            else
            {
                DateTime dt;

                if (!DateTime.TryParse(item.Date.ToString(), out dt))
                {
                    errors.Add("Invalid date format.");
                    return errors;
                }

                if (dt.Date > DateTime.Today)
                    errors.Add("Date cannot be in the future.");

                if (dt.Year < 2000 || dt.Year > 2100)
                    errors.Add("Date year must be between 2000 and 2100.");
            }

            // Unit
            if (item.UnitId <= 0)
                errors.Add("Unit must be selected.");

            // GST
            if (item.GstId <= 0)
                errors.Add("GST must be selected.");

            // Description (optional)
            if (!string.IsNullOrWhiteSpace(item.Description) &&
                item.Description.Length > 300)
                errors.Add("Description cannot exceed 300 characters.");

            return errors;
        }
        //    public List<SalesReturnListItemDto> SearchSalesReturns(
        //DateTime? fromDate,
        //DateTime? toDate
        //)
        //    {
        //        var conn = new SQLiteConnection(_connectionString);
        //        var sql = @"
        //    SELECT
        //        sr.Id,
        //        sr.ReturnNo,
        //        sr.ReturnDate,
        //        sr.InvoiceNo,
        //        c.Name AS CustomerName,
        //        sr.TotalAmount
        //    FROM SalesReturn sr
        //    JOIN Customers c ON c.Id = sr.CustomerId
        //    WHERE 1 = 1
        //";

        //        var dyn = new DynamicParameters();

        //        if (fromDate.HasValue)
        //        {
        //            sql += " AND date(sr.ReturnDate) >= date(@FromDate)";
        //            dyn.Add("@FromDate", fromDate.Value.ToString("yyyy-MM-dd"));
        //        }

        //        if (toDate.HasValue)
        //        {
        //            sql += " AND date(sr.ReturnDate) <= date(@ToDate)";
        //            dyn.Add("@ToDate", toDate.Value.ToString("yyyy-MM-dd"));
        //        }

        //        if (!string.IsNullOrWhiteSpace(searchText))
        //        {
        //            sql += " AND (sr.ReturnNo LIKE @Search OR sr.InvoiceNo LIKE @Search OR c.Name LIKE @Search)";
        //            dyn.Add("@Search", "%" + searchText + "%");
        //        }

        //        sql += " ORDER BY date(sr.ReturnDate) DESC, sr.Id DESC";

        //        return conn.Query<SalesReturnListItemDto>(sql, dyn).ToList();
        //    }
        public List<string> ValidateInventoryDetails(ItemDetails details)
        {
            var errors = new List<string>();

            // ---------- REQUIRED FIELDS ----------

            if (details.SupplierId == 0)
                errors.Add("Select Supplier");


            // 1. HSN Code (required)
            //if (string.IsNullOrWhiteSpace(details.HsnCode))
            //errors.Add("HSN/SAC Code cannot be empty.");

            // 2. Batch No (required)
            if (string.IsNullOrWhiteSpace(details.BatchNo))
                errors.Add("Batch Number cannot be empty.");

            // 3. Ref/Invoice No (optional)
            // Add only if you want it required
            // if (string.IsNullOrWhiteSpace(details.RefNo))
            //     errors.Add("Reference/Invoice Number cannot be empty.");

            // 4. Date (required)
            if (details.Date == null)
            {
                errors.Add("Date is required.");
            }
            else
            {
                DateTime dt;

                // Convert string → DateTime
                if (!DateTime.TryParse(details.Date.ToString(), out dt))
                {
                    errors.Add("Invalid date format.");
                    return errors;
                }

                // NOW dt is a valid DateTime
                if (dt.Date > DateTime.Today)
                    errors.Add("Date cannot be in the future.");

                if (dt.Year < 2000 || dt.Year > 2100)
                    errors.Add("Date year must be between 2000 and 2100.");
            }


            // 5. Quantity (required)
            if (details.Quantity <= 0)
                errors.Add("Quantity must be greater than 0.");

            // 6. Purchase Price (required)
            if (details.PurchasePrice <= 0)
                errors.Add("Purchase Price must be greater than 0.");

            // 7. Discount Percent (0–100 allowed)
            if (details.DiscountPercent < 0 || details.DiscountPercent > 100)
                errors.Add("Discount percent must be between 0 and 100.");

            // 8. Net Purchase Price (computed, but still validate)
            if (details.NetPurchasePrice < 0)
                errors.Add("Net Purchase Price is invalid.");

            // 9. Amount (computed)
            if (details.Amount < 0)
                errors.Add("Amount is invalid.");

            // 10. Sales Price (optional but if present must be valid)
            if (details.SalesPrice < 0)
                errors.Add("Sales Price cannot be negative.");

            // 11. MRP (optional)
            if (details.Mrp < 0)
                errors.Add("MRP cannot be negative.");

            // 12. Goods/Services (Required)
            if (string.IsNullOrWhiteSpace(details.GoodsOrServices))
                errors.Add("Please select Goods/Services.");

            // 13. Description (optional)
            if (!string.IsNullOrWhiteSpace(details.Description) && details.Description.Length > 300)
                errors.Add("Description cannot exceed 300 characters.");

            // ---------- OPTIONAL FIELDS ----------

            // ---------- MfgDate ----------
            if (!string.IsNullOrWhiteSpace(details.MfgDate))
            {
                if (DateTime.TryParse(details.MfgDate, out DateTime mfgDate))
                {
                    if (mfgDate > DateTime.Today)
                        errors.Add("Manufacturing Date cannot be in the future.");
                }
                else
                {
                    errors.Add("Manufacturing Date is invalid.");
                }
            }


            // ---------- ExpDate ----------
            if (!string.IsNullOrWhiteSpace(details.ExpDate))
            {
                // Try to parse ExpDate
                if (DateTime.TryParse(details.ExpDate, out DateTime expDate))
                {
                    // 1. Cannot be in the past
                    if (expDate < DateTime.Today)
                        errors.Add("Expiry Date cannot be in the past.");

                    // 2. Compare with MfgDate if present
                    if (!string.IsNullOrWhiteSpace(details.MfgDate) &&
                        DateTime.TryParse(details.MfgDate, out DateTime mfgDate))
                    {
                        if (expDate < mfgDate)
                            errors.Add("Expiry Date cannot be earlier than Manufacturing Date.");
                    }
                }
                else
                {
                    errors.Add("Expiry Date is invalid.");
                }
            }


            if (!string.IsNullOrWhiteSpace(details.ModelNo) && details.ModelNo.Length > 100)
                errors.Add("Model No cannot exceed 100 characters.");

            if (!string.IsNullOrWhiteSpace(details.Brand) && details.Brand.Length > 100)
                errors.Add("Brand cannot exceed 100 characters.");

            if (!string.IsNullOrWhiteSpace(details.Size) && details.Size.Length > 50)
                errors.Add("Size cannot exceed 50 characters.");

            if (!string.IsNullOrWhiteSpace(details.Color) && details.Color.Length > 50)
                errors.Add("Color cannot exceed 50 characters.");

            if (!string.IsNullOrWhiteSpace(details.Weight) && details.Weight.Length > 50)
                errors.Add("Weight cannot exceed 50 characters.");

            if (!string.IsNullOrWhiteSpace(details.Dimension) && details.Dimension.Length > 100)
                errors.Add("Dimension cannot exceed 100 characters.");

            return errors;
        }


        public InvoiceForReturnDto LoadInvoiceForReturn(int invoiceId)
        {
            var conn = new SQLiteConnection(_connectionString);
            var invoice = conn.QuerySingleOrDefault<InvoiceForReturnDto>(
                "SELECT Id, InvoiceNo, CustomerId, CustomerName FROM Invoice WHERE Id=@id",
                new { id = invoiceId });

            var items = conn.Query<InvoiceReturnItemDto>(@"
        SELECT 
            ii.Id AS InvoiceItemId,
            ii.ItemId,
            it.name,
            ii.BatchNo,
            ii.Qty AS OriginalQty,
            ii.Rate,
            ii.DiscountPercent AS DiscountPercent,
            ii.GstPercent AS GstPercent,
            ii.CgstPercent AS CgstPercent,
            ii.SgstPercent AS SgstPercent,
            ii.IgstPercent AS IgstPercent,
            ii.GstValue AS GstValue,
            ii.LineTotal AS LineSubTotal,
            ii.ReturnedQty,
            (ii.Qty - ii.ReturnedQty) AS AvailableReturnQty
        FROM InvoiceItems ii
        JOIN Item it ON it.Id = ii.ItemId
        WHERE InvoiceId = @id",
                new { id = invoiceId }).ToList();

            invoice.ReturnItems = items;
            return invoice;
        }

        public List<SalesReturnSearchRowDto> SearchSalesReturns(string date)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                //DateTime d = DateTime.Parse(date);
                //DateTime next = d.AddDays(1);

                const string sql = @"
        SELECT 
            SalesReturn.Id,
            ReturnNo,
            ReturnDate,
            InvoiceNo,
            Customers.Name AS CustomerName,
            TotalAmount
        FROM SalesReturn
        INNER JOIN Customers ON Customers.Id = SalesReturn.CustomerId
        WHERE substr(ReturnDate, 1, 10) = @date
        ORDER BY ReturnNo";

                return conn.Query<SalesReturnSearchRowDto>(sql, new { date }).ToList();
            }
        }


        public InvoiceForReturnDto GetInvoiceForReturn(string invoiceNo)
        {
            var conn = new SQLiteConnection(_connectionString);
            const string invoiceSql = @"
        SELECT inv.Id,
               inv.InvoiceNo,
               inv.CustomerId,
               c.Name AS CustomerName
        FROM Invoice inv
        JOIN Customer c ON c.Id = inv.CustomerId
        WHERE inv.InvoiceNo = @InvoiceNo;
    ";

            var invoice = conn.QuerySingleOrDefault<InvoiceForReturnDto>(
                invoiceSql,
                new { InvoiceNo = invoiceNo }
            );

            if (invoice == null)
                return null;

            const string itemsSql = @"
        SELECT
            ii.Id AS InvoiceItemId,
            ii.ItemId,
            it.Name AS ItemName,
            ii.BatchNo,
            ii.Qty AS OriginalQty,
            IFNULL(ii.ReturnedQty, 0) AS ReturnedQty,
            (ii.Qty - IFNULL(ii.ReturnedQty, 0)) AS AvailableReturnQty,
            ii.Rate,
            ii.DiscountPercent,
            ii.GstPercent,
            ii.CgstPercent,
            ii.SgstPercent,
            ii.IgstPercent
        FROM InvoiceItems ii
        JOIN Item it ON it.Id = ii.ItemId
        WHERE ii.InvoiceId = @InvoiceId;
    ";

            invoice.ReturnItems = conn.Query<InvoiceReturnItemDto>(
                itemsSql,
                new { InvoiceId = invoice.Id }
            ).ToList();

            return invoice;
        }


        //    public SalesReturnPrintHeaderDto GetSalesReturnHeader(int id)
        //    {
        //        var conn = new SQLiteConnection(_connectionString);
        //        const string sql = @"
        //    SELECT
        //        sr.Id,
        //        sr.ReturnNo,
        //        sr.ReturnNum,
        //        sr.ReturnDate,
        //        sr.InvoiceNo,
        //        c.Name AS CustomerName,
        //        c.Address AS CustomerAddress,
        //        c.GstNo AS CustomerGstNo,
        //        sr.SubTotal,
        //        sr.TotalTax,
        //        sr.TotalAmount,
        //        sr.RoundOff,
        //        sr.Notes
        //    FROM SalesReturn sr
        //    JOIN Customer c ON c.Id = sr.CustomerId
        //    WHERE sr.Id = @Id;
        //";

        //        return conn.QuerySingleOrDefault<SalesReturnPrintHeaderDto>(sql, new { Id = id });
        //    }
        public SalesReturnLoadDto GetSalesReturn(long salesReturnId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // 1) Header + customer info
                const string headerSql = @"
        SELECT 
            sr.Id,
            sr.ReturnNo,
            sr.ReturnNum,
            sr.ReturnDate,
            sr.InvoiceId,
            sr.InvoiceNo,
            sr.CustomerId,
            c.Name        AS CustomerName,
            c.Phone       AS CustomerPhone,
            c.State       AS CustomerState,
            c.Address     AS CustomerAddress,
            sr.SubTotal,
            sr.TotalTax,
            sr.TotalAmount,
            sr.RoundOff,
            sr.Notes,
            sr.CreatedBy,
            sr.CreatedAt
        FROM SalesReturn sr
        LEFT JOIN Customers c ON c.Id = sr.CustomerId
        WHERE sr.Id = @id;
    ";

                var header = conn.QuerySingleOrDefault<SalesReturnLoadDto>(headerSql, new { id = salesReturnId });
                if (header == null)
                    return null;

                // 2) Items
                const string itemsSql = @"
        SELECT
            sri.InvoiceItemId,
            sri.ItemId,
            it.Name AS ItemName,
            sri.BatchNo,
            sri.Qty,
            sri.Rate,
            sri.DiscountPercent,
            sri.GstPercent,
            sri.GstValue,
            sri.CgstPercent,
            sri.CgstValue,
            sri.SgstPercent,
            sri.SgstValue,
            sri.IgstPercent,
            sri.IgstValue,
            sri.LineSubTotal,
            sri.LineTotal
         FROM SalesReturnItem sri
        JOIN Item it ON it.Id = sri.ItemId
        WHERE SalesReturnId = @id;
    ";

                var items = conn.Query<SalesReturnItemForPrintDto>(itemsSql, new { id = salesReturnId }).ToList();
                header.Items = items;

                return header;
            }
        }

        public List<SalesReturnItemForPrintDto> GetSalesReturnItems(int id)
        {
            var conn = new SQLiteConnection(_connectionString);
            const string sql = @"
        SELECT
            it.Name AS ItemName,
            sri.BatchNo,
            sri.Qty,
            sri.Rate,
            sri.DiscountPercent,
            sri.LineSubTotal,
            sri.GstPercent,
            sri.GstValue,
            sri.LineTotal
        FROM SalesReturnItem sri
        JOIN Item it ON it.Id = sri.ItemId
        WHERE sri.SalesReturnId = @Id
        ORDER BY sri.Id;
    ";

            return conn.Query<SalesReturnItemForPrintDto>(sql, new { Id = id }).ToList();
        }
        public List<InvoiceSearchRowDto> SearchInvoicesForReturn(string date)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                DateTime d = DateTime.Parse(date);
                DateTime next = d.AddDays(1);

                const string sql = @"
        SELECT Id, InvoiceNo, InvoiceDate, CustomerName, TotalAmount
        FROM Invoice
        WHERE InvoiceDate >= @start AND InvoiceDate < @end
        ORDER BY InvoiceNo";

                return conn.Query<InvoiceSearchRowDto>(
                    sql,
                    new { start = d, end = next }
                ).ToList();
            }
        }

        public List<SupplierDto> SearchSuppliers(string keyword)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var result = new List<SupplierDto>();
                keyword = keyword ?? string.Empty;
                string like = "%" + keyword + "%";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
            SELECT SupplierId, SupplierName, ContactPerson, Mobile,
                   Email, GSTIN, Address, City, Pincode,OpeningBalance,Balance,
                   CreatedBy, CreatedAt,state
            FROM Suppliers
            WHERE (@kw = '' 
                   OR SupplierName LIKE @like 
                   OR Mobile LIKE @like 
                   OR City LIKE @like 
                   OR GSTIN LIKE @like)
            ORDER BY SupplierName;
        ";

                    cmd.Parameters.AddWithValue("@kw", keyword);
                    cmd.Parameters.AddWithValue("@like", like);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new SupplierDto
                            {
                                SupplierId = reader.GetInt64(0),
                                SupplierName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Mobile = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                GSTIN = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                                City = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Pincode = reader.IsDBNull(8) ? null : reader.GetString(8),
                                OpeningBalance = reader.IsDBNull(9) ? (decimal?)null : reader.GetDecimal(9),
                                Balance = reader.IsDBNull(10) ? (decimal?)null : reader.GetDecimal(10),
                                CreatedBy = reader.IsDBNull(11) ? null : reader.GetString(11),
                                CreatedAt = reader.IsDBNull(12) ? null : reader.GetString(12),
                                State = reader.IsDBNull(13) ? null : reader.GetString(13)
                            });
                        }
                    }
                }
                return result;
            }

        }

        public SupplierDto GetSupplier(long supplierId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
            SELECT SupplierId, SupplierName, ContactPerson, Mobile,
                   Email, GSTIN, Address, City, Pincode,openingBalance,Balance,
                   CreatedBy, CreatedAt,state
            FROM Suppliers
            WHERE SupplierId = @id;
        ";

                    cmd.Parameters.AddWithValue("@id", supplierId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new SupplierDto
                            {
                                SupplierId = reader.GetInt64(0),
                                SupplierName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Mobile = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                GSTIN = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                                City = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Pincode = reader.IsDBNull(8) ? null : reader.GetString(8),
                                OpeningBalance = reader.IsDBNull(9) ? (decimal?)null : reader.GetDecimal(9),
                                Balance = reader.IsDBNull(10) ? (decimal?)null : reader.GetDecimal(10),
                                CreatedBy = reader.IsDBNull(11) ? null : reader.GetString(11),
                                CreatedAt = reader.IsDBNull(12) ? null : reader.GetString(12),
                                State = reader.IsDBNull(13) ? null : reader.GetString(13)
                            };
                        }
                    }
                }

                return null;
            }
        }
        public long SaveSupplier(SupplierDto supplier)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                if (supplier == null) throw new ArgumentNullException(nameof(supplier));
                //validation block starts


                // 👉 Mandatory validation
                if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                    throw new Exception("Supplier Name is required.");

                // 👉 Optional but safe validation checks
                if (!string.IsNullOrWhiteSpace(supplier.Email))
                {
                    var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                    if (!Regex.IsMatch(supplier.Email, emailPattern))
                        throw new Exception("Invalid Email format.");
                }

                if (!string.IsNullOrWhiteSpace(supplier.Mobile))
                {
                    if (supplier.Mobile.Length != 10 || !supplier.Mobile.All(char.IsDigit))
                        throw new Exception("Mobile number must be 10 digits.");
                }

                if (!string.IsNullOrWhiteSpace(supplier.GSTIN))
                {
                    if (supplier.GSTIN.Length != 15)
                        throw new Exception("GSTIN must be 15 characters.");
                }

                // 🛑 DO NOT trust Balance from UI — protect from tampering
                if (supplier.SupplierId > 0)
                {
                    var dbSup = GetSupplier(supplier.SupplierId);
                    if (dbSup != null)
                        supplier.Balance = dbSup.Balance; // preserve original
                }
                if (string.IsNullOrWhiteSpace(supplier.State))
                {
                    throw new Exception("State is required.");
                }

                // must be valid India state
                string[] VALID_STATES = new[]
                {
    "Andhra Pradesh","Arunachal Pradesh","Assam","Bihar","Chhattisgarh",
    "Goa","Gujarat","Haryana","Himachal Pradesh","Jharkhand","Karnataka",
    "Kerala","Madhya Pradesh","Maharashtra","Manipur","Meghalaya","Mizoram",
    "Nagaland","Odisha","Punjab","Rajasthan","Sikkim","Tamil Nadu","Telangana",
    "Tripura","Uttar Pradesh","Uttarakhand","West Bengal","Delhi",
    "Jammu & Kashmir","Ladakh","Puducherry","Andaman & Nicobar",
    "Chandigarh","Dadra & Nagar Haveli","Daman & Diu","Lakshadweep"
};

                if (!VALID_STATES.Contains(supplier.State))
                {
                    throw new Exception("Invalid State selected.");
                }

                //validation block ends



                if (supplier.SupplierId == 0)
                {
                    // INSERT
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
    INSERT INTO Suppliers
    (SupplierName, ContactPerson, Mobile, Email,
     GSTIN, Address, City, Pincode,
     OpeningBalance, Balance,
     CreatedBy, CreatedAt,state)
    VALUES
    (@name, @cp, @mobile, @email,
     @gstin, @addr, @city, @pincode,
     @ob, @ob,
     @cby, @cat,@state);
    SELECT last_insert_rowid();
";
                        cmd.Parameters.AddWithValue("@ob", supplier.OpeningBalance ?? 0);
                        cmd.Parameters.AddWithValue("@name", supplier.SupplierName ?? "");
                        cmd.Parameters.AddWithValue("@cp", supplier.ContactPerson ?? "");
                        cmd.Parameters.AddWithValue("@mobile", supplier.Mobile ?? "");
                        cmd.Parameters.AddWithValue("@email", supplier.Email ?? "");
                        cmd.Parameters.AddWithValue("@gstin", supplier.GSTIN ?? "");
                        cmd.Parameters.AddWithValue("@addr", supplier.Address ?? "");
                        cmd.Parameters.AddWithValue("@city", supplier.City ?? "");
                        cmd.Parameters.AddWithValue("@pincode", supplier.Pincode ?? "");
                        cmd.Parameters.AddWithValue("@cby", supplier.CreatedBy ?? "");
                        cmd.Parameters.AddWithValue("@cat", supplier.CreatedAt.ToString());
                        cmd.Parameters.AddWithValue("@state", supplier.State.ToString());

                        var id = (long)(long)cmd.ExecuteScalar();
                        supplier.SupplierId = id;
                        return id;
                    }
                }
                else
                {
                    // UPDATE (CreatedBy/CreatedAt unchanged)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
    UPDATE Suppliers SET
        SupplierName = @name,
        ContactPerson = @cp,
        Mobile = @mobile,
        Email = @email,
        GSTIN = @gstin,
        Address = @addr,
        City = @city,
        Pincode = @pincode,
State=@state
    WHERE SupplierId = @id;
";

                        cmd.Parameters.AddWithValue("@name", supplier.SupplierName ?? "");
                        cmd.Parameters.AddWithValue("@cp", supplier.ContactPerson ?? "");
                        cmd.Parameters.AddWithValue("@mobile", supplier.Mobile ?? "");
                        cmd.Parameters.AddWithValue("@email", supplier.Email ?? "");
                        cmd.Parameters.AddWithValue("@gstin", supplier.GSTIN ?? "");
                        cmd.Parameters.AddWithValue("@addr", supplier.Address ?? "");
                        cmd.Parameters.AddWithValue("@city", supplier.City ?? "");
                        cmd.Parameters.AddWithValue("@pincode", supplier.Pincode ?? "");
                        cmd.Parameters.AddWithValue("@state", supplier.State ?? "");
                        cmd.Parameters.AddWithValue("@id", supplier.SupplierId);

                        cmd.ExecuteNonQuery();
                        return supplier.SupplierId;
                    }
                }
            }
        }

        public bool DeleteSupplier(long supplierId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"DELETE FROM Suppliers WHERE SupplierId = @id;";
                    cmd.Parameters.AddWithValue("@id", supplierId);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }
        public SupplierDto GetSupplierById(long supplierId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.CommandText = @"
            SELECT SupplierId, SupplierName, ContactPerson, Mobile, Email,
                   GSTIN, Address, City, State, Pincode,
                   OpeningBalance, Balance
            FROM Suppliers
            WHERE SupplierId = @id";

                cmd.Parameters.AddWithValue("@id", supplierId);

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new SupplierDto
                        {
                            SupplierId = r.GetInt64(0),
                            SupplierName = r.GetString(1),
                            ContactPerson = r.IsDBNull(2) ? "" : r.GetString(2),
                            Mobile = r.IsDBNull(3) ? "" : r.GetString(3),
                            Email = r.IsDBNull(4) ? "" : r.GetString(4),
                            GSTIN = r.IsDBNull(5) ? "" : r.GetString(5),
                            Address = r.IsDBNull(6) ? "" : r.GetString(6),
                            City = r.IsDBNull(7) ? "" : r.GetString(7),
                            State = r.IsDBNull(8) ? "" : r.GetString(8),
                            Pincode = r.IsDBNull(9) ? "" : r.GetString(9),
                            OpeningBalance = r.IsDBNull(10) ? 0 : r.GetDecimal(10),
                            Balance = r.IsDBNull(11) ? 0 : r.GetDecimal(11)
                        };
                    }
                }

                return null;
            }
        }

        public List<SupplierDto> GetAllSuppliers()
        {
            var list = new List<SupplierDto>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = @"SELECT SupplierId, SupplierName FROM Suppliers ORDER BY SupplierName ASC";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SupplierDto
                        {
                            SupplierId = reader.GetInt64(0),
                            SupplierName = reader.GetString(1)
                        });
                    }
                }
            }
            return list;
        }
        public List<PurchaseItemForDateDto> SearchPurchaseItemsByDate(DateTime date)
        {
            var list = new List<PurchaseItemForDateDto>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"SELECT 
            d.id AS ItemDetailsId,
            d.item_id AS ItemId,
            i.name AS ItemName,
            d.refno AS InvoiceNo,
            d.date AS PurchaseDate,
            d.SupplierId,
            s.suppliername AS SupplierName,
            d.quantity
          FROM ItemDetails d
          JOIN Item i ON i.id = d.item_id
          LEFT JOIN Suppliers s ON s.supplierId = d.SupplierId
          WHERE DATE(d.date) = DATE(@date)
          ORDER BY d.date, d.refno;";

                    cmd.Parameters.AddWithValue("@date", date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PurchaseItemForDateDto
                            {
                                ItemDetailsId = reader.GetInt64(0),
                                ItemId = reader.GetInt64(1),
                                ItemName = reader.GetString(2),
                                InvoiceNo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                PurchaseDate = reader.GetDateTime(4),
                                SupplierId = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                                SupplierName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Quantity = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7))
                            });
                        }
                    }
                }

                return list;
            }
        }
        public PurchaseItemDetailDto LoadPurchaseForReturn(long itemDetailsId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                    @"SELECT 
   d.id AS ItemDetailsId,
   d.item_id AS ItemId,
   i.name AS ItemName,
   i.hsnCode,

   d.batchNo,                  -- NEW
   d.purchasePrice,
   d.discountPercent,          -- NEW
   d.netpurchasePrice,         -- NEW
   d.quantity,

   (SELECT IFNULL(SUM(r.returnQty),0)
    FROM PurchaseReturn r
    WHERE r.itemDetailsId = d.id) AS ReturnedQty,

   d.refno,
   d.date,
   d.SupplierId,
   s.suppliername AS SupplierName,
   g.GstPercent
FROM ItemDetails d
JOIN Item i ON i.id = d.item_id
LEFT JOIN Suppliers s ON s.supplierId = d.SupplierId
LEFT JOIN GstMaster g ON g.Id = i.gstid
WHERE d.id = @id;
";

                    cmd.Parameters.AddWithValue("@id", itemDetailsId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return new PurchaseItemDetailDto
                            {
                                ItemDetailsId = r.GetInt64(0),
                                ItemId = r.GetInt64(1),
                                ItemName = r.GetString(2),
                                HsnCode = r.IsDBNull(3) ? "" : r.GetString(3),
                                BatchNo = r.IsDBNull(4) ? "" : r.GetString(4),
                                PurchasePrice = Convert.ToDecimal(r.GetValue(5)),
                                DiscountPercent = Convert.ToDecimal(r.GetValue(6)),
                                NetPurchasePrice = Convert.ToDecimal(r.GetValue(7)),
                                Quantity = Convert.ToDecimal(r.GetValue(8)),
                                AlreadyReturnedQty = Convert.ToDecimal(r.GetValue(9)),
                               
                                InvoiceNo = r.IsDBNull(8) ? "" : r.GetString(10),
                                PurchaseDate = r.GetDateTime(11),
                                SupplierId = Convert.ToInt64(r.GetValue(12)),
                                SupplierName = r.IsDBNull(11) ? "" : r.GetString(13),
                                GstPercent = r.IsDBNull(12) ? 0 : Convert.ToDecimal(r.GetValue(14)),
                                
                            };
                        }
                    }
                }

                return null;
            }
        }
        public PurchaseReturnResult SavePurchaseReturn(PurchaseReturnDto dto)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;

                    // 1️⃣ Get next ReturnNum
                    long nextNum;
                    using (var cmdNum = conn.CreateCommand())
                    {
                        cmdNum.Transaction = tran;
                        cmdNum.CommandText = "SELECT IFNULL(MAX(ReturnNum), 0) + 1 FROM PurchaseReturn";
                        nextNum = Convert.ToInt64(cmdNum.ExecuteScalar());
                    }

                    string nextNo = $"PR/{nextNum}";

                    // 2️⃣ Insert
                    cmd.CommandText =
                    @"INSERT INTO PurchaseReturn
            (ReturnNo, ReturnNum, itemDetailsId, itemId, returnDate, returnQty,
             rate, discountPercent, netrate, batchno, gstPercent,
             amount, cgst, sgst, igst, totalAmount,
             SupplierId, remarks, createdat, createdby)
          VALUES
            (@ReturnNo, @ReturnNum, @itemDetailsId, @itemId, @returnDate, @returnQty,
             @rate, @discountPercent, @netrate, @batchno, @gstPercent,
             @amount, @cgst, @sgst, @igst, @totalAmount,
             @SupplierId, @remarks, DATETIME('now'), @createdBy);

          SELECT last_insert_rowid();";

                    cmd.Parameters.AddWithValue("@ReturnNo", nextNo);
                    cmd.Parameters.AddWithValue("@ReturnNum", nextNum);
                    cmd.Parameters.AddWithValue("@itemDetailsId", dto.ItemDetailsId);
                    cmd.Parameters.AddWithValue("@itemId", dto.ItemId);
                    cmd.Parameters.AddWithValue("@returnDate", dto.ReturnDate);
                    cmd.Parameters.AddWithValue("@returnQty", dto.Qty);

                    cmd.Parameters.AddWithValue("@rate", dto.Rate);
                    cmd.Parameters.AddWithValue("@discountPercent", dto.DiscountPercent);
                    cmd.Parameters.AddWithValue("@netrate", dto.NetRate);
                    cmd.Parameters.AddWithValue("@batchno", dto.BatchNo ?? "");

                    cmd.Parameters.AddWithValue("@gstPercent", dto.GstPercent);
                    cmd.Parameters.AddWithValue("@amount", dto.Amount);
                    cmd.Parameters.AddWithValue("@cgst", dto.Cgst);
                    cmd.Parameters.AddWithValue("@sgst", dto.Sgst);
                    cmd.Parameters.AddWithValue("@igst", dto.Igst);
                    cmd.Parameters.AddWithValue("@totalAmount", dto.TotalAmount);

                    cmd.Parameters.AddWithValue("@SupplierId", dto.SupplierId);
                    cmd.Parameters.AddWithValue("@remarks", dto.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@createdBy", dto.CreatedBy ?? "");

                    long newId = Convert.ToInt64(cmd.ExecuteScalar());
                    ItemLedger ledgerEntry = new ItemLedger();
                    ledgerEntry.ItemId = dto.ItemId;
                    ledgerEntry.BatchNo = dto.BatchNo;
                    ledgerEntry.Date = dto.ReturnDate.ToString("yyyy-MM-dd HH:mm:ss");
                    ledgerEntry.TxnType = "Purchase Return";
                    ledgerEntry.RefNo = dto.ReturnNo;
                    ledgerEntry.Qty = dto.Qty;
                    ledgerEntry.Rate = dto.Rate;
                    ledgerEntry.DiscountPercent = dto.DiscountPercent;
                    decimal netRate = dto.Rate - (dto.Rate * dto.DiscountPercent / 100);
                    ledgerEntry.NetRate = netRate;
                    ledgerEntry.TotalAmount = dto.TotalAmount;
                    ledgerEntry.Remarks = "Purchase Return";
                    ledgerEntry.CreatedBy = dto.CreatedBy;
                    AddItemLedger(ledgerEntry, conn, tran);
                    UpdateItemBalanceSales(ledgerEntry, conn, tran);
                    tran.Commit();

                    return new PurchaseReturnResult
                    {
                        ReturnId = newId,
                        ReturnNum = nextNum,
                        ReturnNo = nextNo
                    };
                }
            }
        }
        public List<object> SearchPurchaseReturns(DateTime date)
        {
            var list = new List<object>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // Use DATE(...) to compare only the date part
                    cmd.CommandText = @"
            SELECT 
                pr.id AS ReturnId,
                pr.ReturnNo,
                pr.returnDate,
                d.refno AS InvoiceNo,
                COALESCE(s.supplierName, '') AS SupplierName,
                pr.totalAmount
            FROM PurchaseReturn pr
            LEFT JOIN ItemDetails d ON pr.itemDetailsId = d.id
            LEFT JOIN Suppliers s ON pr.SupplierId = s.supplierId
            WHERE DATE(pr.returnDate) = DATE(@date)
            ORDER BY pr.returnDate DESC, pr.id DESC;
        ";

                    cmd.Parameters.AddWithValue("@date", date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetInt64(0);
                            var returnNo = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            var dt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                            var invoiceNo = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            var supplierName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            var totalAmount = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5));

                            list.Add(new
                            {
                                Id = id,
                                ReturnNo = returnNo,
                                ReturnDate = dt?.ToString("yyyy-MM-dd") ?? "",
                                InvoiceNo = invoiceNo,
                                SupplierName = supplierName,
                                TotalAmount = totalAmount
                            });
                        }
                    }
                }

                return list;
            }
        }

        public object GetPurchaseReturnDetail(long returnId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT 
                    pr.id,
                    pr.ReturnNo,
                    pr.returnDate,
                    d.refno AS InvoiceNo,
                    s.supplierName AS SupplierName,
                    pr.remarks,
                    i.name AS ItemName,
                    pr.batchNo,
                    pr.returnQty,
                    pr.Rate,
                    pr.gstPercent,
                    pr.cgst,
                    pr.sgst,
                    pr.amount,
                    pr.totalAmount
                FROM PurchaseReturn pr
                LEFT JOIN ItemDetails d ON pr.itemDetailsId = d.id
                LEFT JOIN Suppliers s ON pr.SupplierId = s.supplierId
                LEFT JOIN Item i ON pr.itemId = i.id
                WHERE pr.id = @id;
            ";

                    cmd.Parameters.AddWithValue("@id", returnId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;

                        decimal cgst = Convert.ToDecimal(r["cgst"]);
                        decimal sgst = Convert.ToDecimal(r["sgst"]);

                        return new
                        {
                            Id = r.GetInt64(0),
                            ReturnNo = r.IsDBNull(1) ? "" : r.GetString(1),
                            ReturnDate = r.IsDBNull(2) ? "" : r.GetDateTime(2).ToString("yyyy-MM-dd"),
                            InvoiceNo = r.IsDBNull(3) ? "" : r.GetString(3),
                            SupplierName = r.IsDBNull(4) ? "" : r.GetString(4),
                            Notes = r.IsDBNull(5) ? "" : r.GetString(5),

                            // item fields
                            ItemName = r.IsDBNull(6) ? "" : r.GetString(6),
                            BatchNo = r.IsDBNull(7) ? "" : r.GetString(7),
                            Qty = Convert.ToDecimal(r["returnQty"]),
                            Rate = Convert.ToDecimal(r["Rate"]),
                            GstPercent = Convert.ToDecimal(r["gstPercent"]),
                            GstValue = cgst + sgst,
                            LineSubTotal = Convert.ToDecimal(r["amount"]),
                            LineTotal = Convert.ToDecimal(r["totalAmount"])
                        };
                    }
                }
            }
        }
        public int GetNextBatchNumForItem(long itemId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT IFNULL(MAX(batchNum), 0) + 1 FROM PurchaseItem WHERE ItemId = @itemId";
                    cmd.Parameters.AddWithValue("@itemId", itemId);
                    var val = cmd.ExecuteScalar();
                    return Convert.ToInt32(val);
                }
            }
        }
        public string SavePurchaseInvoice(PurchaseInvoiceDto dto)
        {
            using (var conn = new SQLiteConnection(_connectionString)) 
            { 
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1) Insert header
                        using (var cmd = conn.CreateCommand())
                        {
                            //long nextInvoiceNum = GetNextPurchaseInvoiceNum();

                            cmd.Transaction = tran;
                            cmd.CommandText = @"
INSERT INTO PurchaseHeader (InvoiceNo,InvoiceNum, InvoiceDate, SupplierId, TotalAmount, TotalTax, RoundOff, Notes, CreatedBy, CreatedAt)
VALUES (@InvoiceNo,@InvoiceNum, @InvoiceDate, @SupplierId, @TotalAmount, @TotalTax, @RoundOff, @Notes, @CreatedBy, DATETIME('now'));
SELECT last_insert_rowid();
";
                            cmd.Parameters.AddWithValue("@InvoiceNo", dto.InvoiceNo);
                            cmd.Parameters.AddWithValue("@InvoiceNum", dto.InvoiceNum);
                            cmd.Parameters.AddWithValue(
                            "@InvoiceDate",
                            string.IsNullOrWhiteSpace(dto.InvoiceDate)
                                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                                : dto.InvoiceDate
                        );


                            cmd.Parameters.AddWithValue("@SupplierId", dto.SupplierId);
                            cmd.Parameters.AddWithValue("@TotalAmount", dto.TotalAmount);
                            cmd.Parameters.AddWithValue("@TotalTax", dto.TotalTax);
                            cmd.Parameters.AddWithValue("@RoundOff", dto.RoundOff);
                            cmd.Parameters.AddWithValue("@Notes", dto.Notes ?? "");
                            cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy ?? "");

                            long purchaseId = Convert.ToInt64(cmd.ExecuteScalar());

                            // 2) For each item, insert PurchaseItem + PurchaseItemDetails
                            foreach (var it in dto.Items)
                            {
                                // get next batchNum
                                int nextBatchNum;
                                using (var cmdBatch = conn.CreateCommand())
                                {
                                    cmdBatch.Transaction = tran;
                                    cmdBatch.CommandText = "SELECT IFNULL(MAX(batchNum), 0) + 1 FROM PurchaseItem WHERE ItemId = @ItemId";
                                    cmdBatch.Parameters.AddWithValue("@ItemId", it.ItemId);
                                    nextBatchNum = Convert.ToInt32(cmdBatch.ExecuteScalar());
                                }

                                // get itemcode for formatting batchNo (ITEMCODE-B{n})
                                string itemCode = "";
                                using (var cmdItem = conn.CreateCommand())
                                {
                                    cmdItem.Transaction = tran;
                                    cmdItem.CommandText = "SELECT itemcode FROM Item WHERE id = @ItemId LIMIT 1";
                                    cmdItem.Parameters.AddWithValue("@ItemId", it.ItemId);
                                    var o = cmdItem.ExecuteScalar();
                                    itemCode = o == null ? $"I{it.ItemId}" : o.ToString();
                                }

                                var batchNo = $"{itemCode}-B{nextBatchNum}";

                                // Insert PurchaseItem
                                using (var cmdIns = conn.CreateCommand())
                                {
                                    cmdIns.Transaction = tran;
                                    cmdIns.CommandText = @"
INSERT INTO PurchaseItem
(PurchaseId, ItemId, batchNum, batchNo, Qty, Rate, DiscountPercent, NetRate, GstPercent,GstValue, CgstPercent,CgstValue, SgstPercent,SgstValue, IgstPercent,IgstValue, LineSubTotal, LineTotal,Notes)
VALUES
(@PurchaseId, @ItemId, @batchNum, @batchNo, @Qty, @Rate, @DiscountPercent, @NetRate, @GstPercent, @GstValue, @CgstPercent,@CgstValue, @SgstPercent,@SgstValue, @IgstPercent,@IgstValue, @LineSubTotal, @LineTotal,@Notes);
SELECT last_insert_rowid();
";
                                    cmdIns.Parameters.AddWithValue("@PurchaseId", purchaseId);
                                    cmdIns.Parameters.AddWithValue("@ItemId", it.ItemId);
                                    cmdIns.Parameters.AddWithValue("@batchNum", nextBatchNum);
                                    cmdIns.Parameters.AddWithValue("@batchNo", batchNo);
                                    cmdIns.Parameters.AddWithValue("@Qty", it.Qty);
                                    cmdIns.Parameters.AddWithValue("@Rate", it.Rate);
                                    cmdIns.Parameters.AddWithValue("@DiscountPercent", it.DiscountPercent);
                                    cmdIns.Parameters.AddWithValue("@NetRate", it.NetRate);
                                    cmdIns.Parameters.AddWithValue("@GstPercent", it.GstPercent);
                                    cmdIns.Parameters.AddWithValue("@GstValue", it.GstValue);
                                    cmdIns.Parameters.AddWithValue("@CgstPercent", it.CgstPercent);
                                    cmdIns.Parameters.AddWithValue("@CgstValue", it.CgstValue);
                                    cmdIns.Parameters.AddWithValue("@SgstPercent", it.SgstPercent);
                                    cmdIns.Parameters.AddWithValue("@SgstValue", it.SgstValue);
                                    cmdIns.Parameters.AddWithValue("@IgstPercent", it.IgstPercent);
                                    cmdIns.Parameters.AddWithValue("@IgstValue", it.IgstValue);
                                    cmdIns.Parameters.AddWithValue("@LineSubTotal", it.LineSubTotal);
                                    cmdIns.Parameters.AddWithValue("@LineTotal", it.LineTotal);
                                    cmdIns.Parameters.AddWithValue("@Notes", it.Notes);

                                    long purchaseItemId = Convert.ToInt64(cmdIns.ExecuteScalar());

                                    // 3) Insert PurchaseItemDetails if optional metadata provided
                                    if (it.SalesPrice.HasValue || it.Mrp.HasValue || !string.IsNullOrEmpty(it.Description))
                                    {
                                        using (var cmdDet = conn.CreateCommand())
                                        {
                                            cmdDet.Transaction = tran;
                                            cmdDet.CommandText = @"
INSERT INTO PurchaseItemDetails
(PurchaseItemId, salesPrice, mrp, description, mfgdate, expdate, modelno, brand, size, color, weight, dimension, createdby, createdat)
VALUES
(@PurchaseItemId, @SalesPrice, @Mrp, @Description, @MfgDate, @ExpDate, @ModelNo, @Brand, @Size, @Color, @Weight, @Dimension, @CreatedBy, DATETIME('now'));
";
                                            cmdDet.Parameters.AddWithValue("@PurchaseItemId", purchaseItemId);
                                            cmdDet.Parameters.AddWithValue("@SalesPrice", (object)it.SalesPrice ?? DBNull.Value);
                                            cmdDet.Parameters.AddWithValue("@Mrp", (object)it.Mrp ?? DBNull.Value);
                                            cmdDet.Parameters.AddWithValue("@Description", it.Description ?? "");
                                            cmdDet.Parameters.AddWithValue("@MfgDate", it.MfgDate ?? "");
                                            cmdDet.Parameters.AddWithValue("@ExpDate", it.ExpDate ?? "");
                                            cmdDet.Parameters.AddWithValue("@ModelNo", it.ModelNo ?? "");
                                            cmdDet.Parameters.AddWithValue("@Brand", it.Brand ?? "");
                                            cmdDet.Parameters.AddWithValue("@Size", it.Size ?? "");
                                            cmdDet.Parameters.AddWithValue("@Color", it.Color ?? "");
                                            cmdDet.Parameters.AddWithValue("@Weight", (object)it.Weight ?? DBNull.Value);
                                            cmdDet.Parameters.AddWithValue("@Dimension", it.Dimension ?? "");
                                            cmdDet.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy ?? "");
                                            cmdDet.ExecuteNonQuery();
                                        }
                                    } // end insert details
                                } // end insert purchaseitem
                                ItemLedger itemledger = new ItemLedger();
                                itemledger.ItemId = (int)it.ItemId;
                                itemledger.BatchNo = it.BatchNo;
                                itemledger.Date = dto.InvoiceDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
                                    
                                itemledger.TxnType = "Purchase";
                                itemledger.RefNo = dto.InvoiceNo ?? "";
                                itemledger.Qty = it.Qty;
                                itemledger.Rate = it.Rate;
                                itemledger.DiscountPercent = it.DiscountPercent;
                                itemledger.NetRate = it.NetRate;
                                itemledger.TotalAmount = it.LineTotal;
                                itemledger.Remarks = it.Description;
                                itemledger.CreatedBy = dto.CreatedBy;
                                itemledger.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                AddItemLedger(itemledger, conn, tran);
                                UpdateItemBalance(itemledger, conn, tran);
                            } // end foreach item

                            tran.Commit();
                            return dto.InvoiceNo;
                        } // end using cmd header
                    }

                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
        public int GetNextPurchaseInvoiceNum()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT IFNULL(MAX(InvoiceNum), 0) + 1 FROM PurchaseHeader";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public string GetFinancialYear()
        {
            var today = DateTime.Now;
            int year = today.Month >= 4 ? today.Year : today.Year - 1;

            return $"{year}-{(year + 1).ToString().Substring(2)}";
        }
        public (int nextNum, string fy) GetNextPurchaseInvoiceNumFY()
        {
            string fy = GetFinancialYear();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();

                // Check if entry exists for this financial year
                cmd.CommandText = "SELECT LastNumber FROM InvoiceCounters WHERE YearRange=@yr";
                cmd.Parameters.AddWithValue("@yr", fy);
                var result = cmd.ExecuteScalar();

                int next = 1;

                if (result != null)
                {
                    next = Convert.ToInt32(result) + 1;
                }

                // Update or Insert
                cmd = conn.CreateCommand();
                if (result == null)
                {
                    cmd.CommandText = "INSERT INTO InvoiceCounters (YearRange, LastNumber) VALUES (@yr, @num)";
                }
                else
                {
                    cmd.CommandText = "UPDATE InvoiceCounters SET LastNumber=@num WHERE YearRange=@yr";
                }

                cmd.Parameters.AddWithValue("@yr", fy);
                cmd.Parameters.AddWithValue("@num", next);
                cmd.ExecuteNonQuery();

                return (next, fy);
            }
        }
        public PurchaseInvoiceDto GetPurchaseInvoiceDto(long purchaseId)
        {
            var dto = new PurchaseInvoiceDto();

            using (var con = new SQLiteConnection(_connectionString))
            {
                con.Open();

                // ------------------------------
                // 1) LOAD PURCHASE HEADER
                // ------------------------------
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT 
    PurchaseId,
    InvoiceNo,
    InvoiceNum,
    InvoiceDate,
    SupplierId,
    TotalAmount,
    TotalTax,
    RoundOff,
    Notes,
    CreatedBy
FROM PurchaseHeader
WHERE PurchaseId = @PurchaseId";

                    cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                            return null; // no invoice

                        dto.PurchaseId = rd.GetInt64(0);
                        dto.InvoiceNo = rd["InvoiceNo"]?.ToString();
                        dto.InvoiceNum = Convert.ToInt64(rd["InvoiceNum"]);
                        dto.InvoiceDate = rd["InvoiceDate"]?.ToString();
                        dto.SupplierId = Convert.ToInt64(rd["SupplierId"]);
                        dto.TotalAmount = Convert.ToDecimal(rd["TotalAmount"]);
                        dto.TotalTax = Convert.ToDecimal(rd["TotalTax"]);
                        dto.RoundOff = Convert.ToDecimal(rd["RoundOff"]);
                        dto.Notes = rd["Notes"]?.ToString();
                        dto.CreatedBy = rd["CreatedBy"]?.ToString();
                    }
                }

                // ------------------------------
                // 2) LOAD PURCHASE ITEMS + OPTIONAL DETAILS
                // ------------------------------
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    pi.PurchaseItemId,
    pi.ItemId,
    pi.Qty,
    pi.Rate,
    pi.DiscountPercent,
    pi.NetRate,
    pi.GstPercent,
    pi.GstValue,
    pi.CgstPercent,
    pi.CgstValue,
    pi.SgstPercent,
    pi.SgstValue,
    pi.IgstPercent,
    pi.IgstValue,
    pi.LineSubTotal,
    pi.LineTotal,
    pi.Notes,
    pi.BatchNum,
    pi.BatchNo,

    -- OPTIONAL FIELDS (from PurchaseItemDetails)
    pid.SalesPrice,
    pid.Mrp,
    pid.Description,
    pid.MfgDate,
    pid.ExpDate,
    pid.ModelNo,
    pid.Brand,
    pid.Size,
    pid.Color,
    pid.Weight,
    pid.Dimension

FROM PurchaseItem pi
LEFT JOIN PurchaseItemDetails pid 
       ON pid.PurchaseItemId = pi.PurchaseItemId

WHERE pi.PurchaseId = @PurchaseId
ORDER BY pi.PurchaseItemId ASC";

                    cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var item = new PurchaseInvoiceItemDto
                            {
                                PurchaseItemId = rd.GetInt64(0),
                                ItemId = rd.GetInt64(1),
                                Qty = Convert.ToDecimal(rd["Qty"]),
                                Rate = Convert.ToDecimal(rd["Rate"]),
                                DiscountPercent = Convert.ToDecimal(rd["DiscountPercent"]),
                                NetRate = Convert.ToDecimal(rd["NetRate"]),
                                GstPercent = Convert.ToDecimal(rd["GstPercent"]),
                                GstValue = Convert.ToDecimal(rd["GstValue"]),
                                CgstPercent = Convert.ToDecimal(rd["CgstPercent"]),
                                CgstValue = Convert.ToDecimal(rd["CgstValue"]),
                                SgstPercent = Convert.ToDecimal(rd["SgstPercent"]),
                                SgstValue = Convert.ToDecimal(rd["SgstValue"]),
                                IgstPercent = Convert.ToDecimal(rd["IgstPercent"]),
                                IgstValue = Convert.ToDecimal(rd["IgstValue"]),

                                LineSubTotal = Convert.ToDecimal(rd["LineSubTotal"]),
                                LineTotal = Convert.ToDecimal(rd["LineTotal"]),
                                Notes = rd["Notes"]?.ToString(),

                                BatchNum = Convert.ToInt32(rd["BatchNum"]),
                                BatchNo = rd["BatchNo"]?.ToString(),

                                // OPTIONAL DETAILS
                                SalesPrice = rd["SalesPrice"] != DBNull.Value ? Convert.ToDecimal(rd["SalesPrice"]) : (decimal?)null,
                                Mrp = rd["Mrp"] != DBNull.Value ? Convert.ToDecimal(rd["Mrp"]) : (decimal?)null,
                                Description = rd["Description"]?.ToString(),
                                MfgDate = rd["MfgDate"]?.ToString(),
                                ExpDate = rd["ExpDate"]?.ToString(),
                                ModelNo = rd["ModelNo"]?.ToString(),
                                Brand = rd["Brand"]?.ToString(),
                                Size = rd["Size"]?.ToString(),
                                Color = rd["Color"]?.ToString(),
                                Weight = rd["Weight"] != DBNull.Value ? Convert.ToDecimal(rd["Weight"]) : (decimal?)null,
                                Dimension = rd["Dimension"]?.ToString()
                            };

                            dto.Items.Add(item);
                        }
                    }
                }
            }

            return dto;
        }

        public object GetPurchaseInvoice(long purchaseId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                // header
                object header = null;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT PurchaseId, InvoiceNo, InvoiceDate, SupplierId, TotalAmount, TotalTax, RoundOff, Notes, CreatedBy, CreatedAt
FROM PurchaseHeader WHERE PurchaseId = @id
";
                    cmd.Parameters.AddWithValue("@id", purchaseId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            header = new
                            {
                                PurchaseId = r.GetInt64(0),
                                InvoiceNo = r.IsDBNull(1) ? "" : r.GetString(1),
                                InvoiceDate = r.IsDBNull(2) ? "" : r.GetDateTime(2).ToString("yyyy-MM-dd"),
                                SupplierId = r.GetInt64(3),
                                TotalAmount = r.IsDBNull(4) ? 0m : Convert.ToDecimal(r.GetValue(4)),
                                TotalTax = r.IsDBNull(5) ? 0m : Convert.ToDecimal(r.GetValue(5)),
                                RoundOff = r.IsDBNull(6) ? 0m : Convert.ToDecimal(r.GetValue(6)),
                                Notes = r.IsDBNull(7) ? "" : r.GetString(7),
                                CreatedBy = r.IsDBNull(8) ? "" : r.GetString(8),
                                CreatedAt = r.IsDBNull(9) ? "" : r.GetDateTime(9).ToString("yyyy-MM-ddTHH:mm:ss")
                            };
                        }
                        else return null;
                    }
                }

                // items
                var items = new List<object>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT pi.PurchaseItemId, pi.ItemId, it.name AS ItemName, pi.batchNum, pi.batchNo, pi.Qty, pi.Rate, pi.DiscountPercent,
       pi.NetRate, pi.GstPercent, pi.Cgst, pi.Sgst, pi.Igst, pi.Amount, pi.TotalAmount
FROM PurchaseItem pi
LEFT JOIN Item it ON pi.ItemId = it.id
WHERE pi.PurchaseId = @id
ORDER BY pi.PurchaseItemId
";
                    cmd.Parameters.AddWithValue("@id", purchaseId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            items.Add(new
                            {
                                PurchaseItemId = r.GetInt64(0),
                                ItemId = r.GetInt64(1),
                                ItemName = r.IsDBNull(2) ? "" : r.GetString(2),
                                BatchNum = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                                BatchNo = r.IsDBNull(4) ? "" : r.GetString(4),
                                Qty = Convert.ToDecimal(r["Qty"]),
                                Rate = Convert.ToDecimal(r["Rate"]),
                                DiscountPercent = Convert.ToDecimal(r["DiscountPercent"]),
                                NetRate = Convert.ToDecimal(r["NetRate"]),
                                GstPercent = Convert.ToDecimal(r["GstPercent"]),
                                Cgst = Convert.ToDecimal(r["Cgst"]),
                                Sgst = Convert.ToDecimal(r["Sgst"]),
                                Igst = Convert.ToDecimal(r["Igst"]),
                                Amount = Convert.ToDecimal(r["Amount"]),
                                TotalAmount = Convert.ToDecimal(r["TotalAmount"])
                            });
                        }
                    }
                }

                return new { Header = header, Items = items };
            }
        }




        private long GetNextPurchaseReturnNumber(SqliteConnection conn, SqliteTransaction tran)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT IFNULL(MAX(ReturnNum), 0) + 1 FROM PurchaseReturn";
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }
    }

}
