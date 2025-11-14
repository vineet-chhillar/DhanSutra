using Dapper;
using DhanSutra.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Threading.Tasks;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

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


            //using (var connection = new SQLiteConnection(_connectionString))
            //    connection.Execute(@"
            //CREATE TABLE IF NOT EXISTS Item (
            //    id INTEGER PRIMARY KEY AUTOINCREMENT,
            //    name TEXT NOT NULL,
            //    itemcode TEXT UNIQUE NOT NULL,
            //    category TEXT,
            //    [date] datetime NULL,
            //    description TEXT NULL,
            //    defaultunit TEXT NULL,
            //    gst REAL NULL,
            //    createdby text NULL,
            //    createdat datetime NULL
            //);

            //CREATE TABLE IF NOT EXISTS ItemDetails (
            //    id INTEGER PRIMARY KEY AUTOINCREMENT,
            //    item_id INTEGER NOT NULL,
            //    hsnCode TEXT,
            //    batchNo TEXT,
            //    quantity INTEGER,
            //    purchasePrice REAL,
            //    salesPrice REAL,
            //    mrp REAL,
            //    goodsOrServices TEXT,
            //    description TEXT,
            //    mfgdate TEXT,
            //    expdate TEXT,
            //    modelno TEXT,
            //    brand TEXT,
            //    size TEXT,
            //    color TEXT,
            //    weight REAL,
            //    dimension TEXT,
            //    FOREIGN KEY (item_id) REFERENCES Item(id) ON DELETE CASCADE
            //);
            //");
        }

        public IEnumerable<Item> GetItems()
        {
            using (var connection = new SQLiteConnection(_connectionString))
                return connection.Query<Item>("SELECT i.Id, i.Name, i.ItemCode, c.CategoryName, \r\n      " +
                    " u.UnitName, g.GstPercent, i.Description, i.[Date]\r\nFROM Item i\r\nLEFT JOIN CategoryMaster c" +
                    " ON i.CategoryId = c.Id\r\nLEFT JOIN UnitMaster u ON i.UnitId = u.Id\r\nLEFT JOIN GstMaster g ON i.GstId = g.Id;");
        }
        public IEnumerable<ItemForInvoice> GetItemsForInvoice()
        {
            using (var connection = new SQLiteConnection(_connectionString))
                return connection.Query<ItemForInvoice>("select i.Id, i.Name, i.ItemCode, d.batchno, \r\n      " +
                    " d.hsncode, d.salesprice , u.unitname, g.gstpercent \r\nfrom itemdetails d\r\ninner join item i" +
                    " on i.id=d.item_id\r\nLEFT JOIN UnitMaster u ON i.UnitId = u.Id\r\nLEFT JOIN GstMaster g ON i.GstId = g.Id\r\n order by i.id;");
        }
        public IEnumerable<ItemDetails> GetItemDetails(int itemId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                Console.Write(itemId.ToString());
                return connection.Query<ItemDetails>(
           "SELECT * FROM ItemDetails WHERE item_id = @ItemId",
           new { ItemId = itemId }
           );
            }
        }
        public void AddItem(Item item)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                // 🧾 Log what we are actually inserting
                Console.WriteLine("📥 AddItem() received:");
                Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
                connection.Execute(
                "INSERT INTO Item (name, itemcode, categoryid,[date], description, unitid, gstid,createdby,createdat) " +
                "VALUES" +
                " (@Name, @ItemCode, @CategoryId,@Date, @Description, @UnitId, @GstId,@CreatedBy,@CreatedAt)", item);
            }
        }

        public bool AddItemDetails(ItemDetails details, SQLiteConnection conn, SQLiteTransaction txn)
        {
            try
            {
                // 🧾 Log what we are actually inserting (for debugging)
                Console.WriteLine("📥 AddItem() received:");
                Console.WriteLine(JsonConvert.SerializeObject(details, Formatting.Indented));

                //using (var connection = new SQLiteConnection(_connectionString))
                //{
                    string sql = @"
            INSERT INTO ItemDetails 
                (
item_id,
hsnCode, 
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
createdat)
            VALUES 
                (
@Item_Id,
@HsnCode,
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
@CreatedAt);
            ";
                // ✅ Dapper executes inside the provided transaction
                int rowsAffected = conn.Execute(sql, details, transaction: txn);

                return rowsAffected > 0;
                //using (var cmd = new SQLiteCommand(sql, conn, txn))
                //{
                //    // Dapper returns the number of rows affected
                //    int rowsAffected = conn.Execute(sql, details);

                //    // ✅ If at least one row was inserted successfully
                //    return rowsAffected > 0;
                //}
                //}
            }
            catch (Exception ex)
            {
                // ❌ Log the error and return false
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
        public bool UpdateItem(int id, string name, string itemCode, int? categoryId, string date, string description, int? unitId, int? gstId)
        {
            string sql = @"
        UPDATE Item
        SET 
            name = @name,
            itemcode = @itemCode,
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
        public List<Item> GetItemList()
        {
            var list = new List<Item>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT id,name FROM Item ORDER BY name ASC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Item
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
                    Item_Id,
                    HsnCode,
                    BatchNo,
                    refno,
                    Date,
                    Quantity,
                    PurchasePrice,
                    discountPercent,
                    netPurchasePrice,
                    amount,
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
                    Dimension
                FROM ItemDetails
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

        // ✅ Update
        //       public bool UpdateInventoryRecord(string itemId,string batchNo, string refno, string hsnCode, string date, string quantity,string purchasePrice, string discountPercent,
        //    string netpurchasePrice,string amount,string salesPrice,string mrp,string goodsOrServices,string description,string mfgDate,string expDate,string modelNo,string brand,
        //    string size,string color,string weight,string dimension,string invbatchno
        //)        {
        //           try
        //           {
        //               using (var conn = new SQLiteConnection(_connectionString))
        //               {
        //                   conn.Open();

        //                   string query = @"
        //                   UPDATE ItemDetails
        //                   SET 
        //                   hsnCode = @HsnCode,
        //                   batchNo = @BatchNo,
        //                   refno=@refno,
        //                   date = @Date,
        //                   quantity = @Quantity,
        //                   purchasePrice = @PurchasePrice,
        //                   discountPercent=@DiscountPercent,
        //                   netpurchasePrice= @NetPurchasePrice,
        //                   amount= @Amount,
        //                   salesPrice = @SalesPrice,
        //                   mrp = @Mrp,
        //                   goodsOrServices = @GoodsOrServices,
        //                   description = @Description,
        //                   mfgdate = @MfgDate,
        //                   expdate = @ExpDate,
        //                   modelno = @ModelNo,
        //                   brand = @Brand,
        //                   size = @Size,
        //                   color = @Color,
        //                   weight = @Weight,
        //                   dimension = @Dimension
        //               WHERE item_Id = @ItemId AND batchNo = @invbatchno;
        //           ";

        //                   using (var cmd = new SQLiteCommand(query, conn))
        //                   {
        //                       cmd.Parameters.AddWithValue("@HsnCode", hsnCode);
        //                       cmd.Parameters.AddWithValue("@Date", date);
        //                       cmd.Parameters.AddWithValue("@Quantity", quantity);
        //                       cmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);

        //                       cmd.Parameters.AddWithValue("@DiscountPercent", discountPercent);
        //                       cmd.Parameters.AddWithValue("@NetPurchasePrice", netpurchasePrice);
        //                       cmd.Parameters.AddWithValue("@Amount", amount);


        //                       cmd.Parameters.AddWithValue("@SalesPrice", salesPrice);
        //                       cmd.Parameters.AddWithValue("@Mrp", mrp);
        //                       cmd.Parameters.AddWithValue("@GoodsOrServices", goodsOrServices);
        //                       cmd.Parameters.AddWithValue("@Description", description);
        //                       cmd.Parameters.AddWithValue("@MfgDate", mfgDate);
        //                       cmd.Parameters.AddWithValue("@ExpDate", expDate);
        //                       cmd.Parameters.AddWithValue("@ModelNo", modelNo);
        //                       cmd.Parameters.AddWithValue("@Brand", brand);
        //                       cmd.Parameters.AddWithValue("@Size", size);
        //                       cmd.Parameters.AddWithValue("@Color", color);
        //                       cmd.Parameters.AddWithValue("@Weight", weight);
        //                       cmd.Parameters.AddWithValue("@Dimension", dimension);
        //                       cmd.Parameters.AddWithValue("@ItemId", itemId);
        //                       cmd.Parameters.AddWithValue("@BatchNo", batchNo);
        //                       cmd.Parameters.AddWithValue("@refno", refno);
        //                       cmd.Parameters.AddWithValue("@invbatchno", invbatchno);

        //                       int rows = cmd.ExecuteNonQuery();
        //                       return rows > 0;
        //                   }
        //               }
        //           }
        //           catch (Exception ex)
        //           {
        //               Console.WriteLine("Error updating inventory: " + ex.Message);
        //               return false;
        //           }
        //       }
        public bool UpdateInventoryRecord(
           SQLiteConnection conn,
           SQLiteTransaction tran,
           string itemId, string batchNo, string refno, string hsnCode, string date,
           string quantity, string purchasePrice, string discountPercent,
           string netpurchasePrice, string amount, string salesPrice, string mrp,
           string goodsOrServices, string description, string mfgDate, string expDate,
           string modelNo, string brand, string size, string color, string weight,
           string dimension, string invbatchno)
        {
            try
            {
                string query = @"
            UPDATE ItemDetails
            SET 
                hsnCode = @HsnCode,
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
                dimension = @Dimension
            WHERE item_Id = @ItemId 
              AND batchNo = @invbatchno;
        ";

                using (var cmd = new SQLiteCommand(query, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@HsnCode", hsnCode);
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

        //   public bool UpdateItemLedger(string itemId, string batchNo, string refno, string date, string quantity, string purchasePrice, string discountPercent,
        //string netpurchasePrice, string amount, string description, string invbatchno)
        //   {
        //       try
        //       {
        //           using (var conn = new SQLiteConnection(_connectionString))
        //           {
        //               conn.Open();

        //               string query = @"
        //               UPDATE ItemLedger
        //               SET 
        //               BatchNo = @BatchNo,
        //               refno=@refno,
        //               date = @Date,
        //               Qty = @Quantity,
        //               Rate = @PurchasePrice,
        //               DiscountPercent=@DiscountPercent,
        //               NetRate= @NetPurchasePrice,
        //               TotalAmount= @Amount,
        //               Remarks = @Description
        //               WHERE ItemId = @ItemId AND BatchNo = @invbatchno;
        //       ";

        //               using (var cmd = new SQLiteCommand(query, conn))
        //               {

        //                   cmd.Parameters.AddWithValue("@Date", date);
        //                   cmd.Parameters.AddWithValue("@Quantity", quantity);
        //                   cmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
        //                   cmd.Parameters.AddWithValue("@DiscountPercent", discountPercent);
        //                   cmd.Parameters.AddWithValue("@NetPurchasePrice", netpurchasePrice);
        //                   cmd.Parameters.AddWithValue("@Amount", amount);                                          
        //                   cmd.Parameters.AddWithValue("@Description", description);
        //                   cmd.Parameters.AddWithValue("@ItemId", itemId);
        //                   cmd.Parameters.AddWithValue("@BatchNo", batchNo);
        //                   cmd.Parameters.AddWithValue("@refno", refno);
        //                   cmd.Parameters.AddWithValue("@invbatchno", invbatchno);

        //                   int rows = cmd.ExecuteNonQuery();
        //                   return rows > 0;
        //               }
        //           }
        //       }
        //       catch (Exception ex)
        //       {
        //           Console.WriteLine("Error updating item ledger: " + ex.Message);
        //           return false;
        //       }
        //   }
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

        //public bool UpdateItemBalanceForBatchNo(string itemId, string batchNo,string invbatchno)
        //{
        //    try
        //    {
        //        using (var conn = new SQLiteConnection(_connectionString))
        //        {
        //            conn.Open();

        //            string query = @"
        //            UPDATE ItemBalance
        //            SET 
        //            BatchNo = @BatchNo
        //            WHERE ItemId = @ItemId AND BatchNo = @invbatchno;
        //    ";

        //            using (var cmd = new SQLiteCommand(query, conn))
        //            {

        //                cmd.Parameters.AddWithValue("@ItemId", itemId);
        //                cmd.Parameters.AddWithValue("@BatchNo", batchNo);
        //                cmd.Parameters.AddWithValue("@invbatchno", invbatchno);

        //                int rows = cmd.ExecuteNonQuery();
        //                return rows > 0;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error updating item balance: " + ex.Message);
        //        return false;
        //    }
        //}
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

        //// ✅ Delete
        //public DbResult DeleteInventory(JObject payload)
        //{
        //    try
        //    {
        //        int id = payload["id"]?.Value<int>() ?? 0;
        //        if (id == 0)
        //            return new DbResult { Success = false, Message = "Invalid ID." };

        //        using var conn = new SQLiteConnection(_connectionString);
        //        conn.Open();
        //        using var tx = conn.BeginTransaction();

        //        string sql = "DELETE FROM Inventory WHERE Id = @Id;";
        //        using var cmd = new SQLiteCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@Id", id);

        //        int rows = cmd.ExecuteNonQuery();
        //        tx.Commit();

        //        return new DbResult
        //        {
        //            Success = rows > 0,
        //            Message = rows > 0
        //                ? "🗑️ Record deleted successfully."
        //                : "⚠️ Record not found."
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Delete error: {ex.Message}");
        //        return new DbResult
        //        {
        //            Success = false,
        //            Message = $"Delete failed: {ex.Message}"
        //        };
        //    }
        //}
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

        //public bool UpdateItemBalance_ForChangeInQuantity(string itemId, string batchNo, string invbatchno,string quantity)
        //{
        //    try
        //    {
        //        using (var conn = new SQLiteConnection(_connectionString))
        //        {
        //            conn.Open();
        //            using (var txn = conn.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // 1️⃣ First SQL update
        //                    string query = @"
        //                UPDATE ItemBalance
        //                SET CurrentQtyBatchWise = @Qty,
        //                    LastUpdated = datetime('now','localtime')
        //                WHERE ItemId = @ItemId AND BatchNo = @BatchNo;
        //            ";

        //                    using (var cmd1 = new SQLiteCommand(query, conn, txn))
        //                    {
        //                        cmd1.Parameters.AddWithValue("@Qty", quantity); // using invbatchno as Qty placeholder?
        //                        cmd1.Parameters.AddWithValue("@ItemId", itemId);
        //                        cmd1.Parameters.AddWithValue("@BatchNo", batchNo);
        //                        cmd1.ExecuteNonQuery();
        //                    }

        //                    // 2️⃣ Second SQL update (can target another batch or same)
        //                    string sqlTotal = @"
        //                    UPDATE ItemBalance
        //                    SET 
        //                        CurrentQty = (
        //                            SELECT SUM(CurrentQtyBatchWise)
        //                            FROM ItemBalance AS sub
        //                            WHERE sub.ItemId = @ItemId
        //                        ),
        //                        LastUpdated = datetime('now','localtime')
        //                    WHERE Id = (
        //                        SELECT MAX(Id)
        //                        FROM ItemBalance
        //                        WHERE ItemId = @ItemId
        //                    );
        //                    ";

        //                    using (var cmd2 = new SQLiteCommand(sqlTotal, conn, txn))
        //                    {
        //                        cmd2.Parameters.AddWithValue("@BatchNo", batchNo);
        //                        cmd2.ExecuteNonQuery();
        //                    }

        //                    // ✅ If both succeed, commit
        //                    txn.Commit();
        //                    return true;
        //                }
        //                catch (Exception innerEx)
        //                {
        //                    Console.WriteLine("❌ Error in transaction: " + innerEx.Message);
        //                    txn.Rollback();
        //                    return false;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("❌ Database error: " + ex.Message);
        //        return false;
        //    }
        //}
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


        //public bool UpdateItemBalance_ForChangeInQuantity(string itemId, string batchNo, string invbatchno)
        //{
        //    try
        //    {
        //        using (var conn = new SQLiteConnection(_connectionString))
        //        {
        //            conn.Open();

        //            string query = @"
        //UPDATE ItemBalance
        //SET CurrentQtyBatchWise = @Qty,
        //    LastUpdated = datetime('now','localtime')    
        //        WHERE ItemId = @ItemId and BatchNo=@BatchNo";

        //            using (var cmd = new SQLiteCommand(query, conn))
        //            {

        //                cmd.Parameters.AddWithValue("@ItemId", itemId);
        //                cmd.Parameters.AddWithValue("@BatchNo", batchNo);
        //                cmd.Parameters.AddWithValue("@invbatchno", invbatchno);

        //                int rows = cmd.ExecuteNonQuery();
        //                return rows > 0;
        //            }                    
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error updating item balance: " + ex.Message);
        //        return false;
        //    }
        //}
        //public bool UpdateItemBalance_ForChangeInQuantity(string itemId, string batchNo, string invbatchno)
        //{
        //    try
        //    {
        //        // 1️⃣ Insert or update batch-wise balance
        //        string sql = @"
        //UPDATE ItemBalance
        //SET CurrentQtyBatchWise = @Qty,
        //    LastUpdated = datetime('now','localtime')    
        //        WHERE ItemId = @ItemId and BatchNo=@BatchNo";

        //        using (var cmd = new SQLiteCommand(sql, conn, txn))
        //        {
        //            cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
        //            cmd.Parameters.AddWithValue("@BatchNo", entry.BatchNo ?? "");
        //            cmd.Parameters.AddWithValue("@Qty", entry.Qty);
        //            cmd.ExecuteNonQuery();
        //        }

        //        // 2️⃣ Update total (CurrentQty) only for the latest record
        //        string sqlTotal = @"
        //UPDATE ItemBalance
        //SET 
        //    CurrentQty = (
        //        SELECT SUM(CurrentQtyBatchWise)
        //        FROM ItemBalance AS sub
        //        WHERE sub.ItemId = @ItemId
        //    ),
        //    LastUpdated = datetime('now','localtime')
        //WHERE Id = (
        //    SELECT MAX(Id)
        //    FROM ItemBalance
        //    WHERE ItemId = @ItemId
        //);
        //";

        //        using (var cmd = new SQLiteCommand(sqlTotal, conn, txn))
        //        {
        //            cmd.Parameters.AddWithValue("@ItemId", entry.ItemId);
        //            cmd.ExecuteNonQuery();
        //        }

        //        // ✅ If both SQL operations succeed
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("❌ Error in UpdateItemBalance: " + ex.Message);
        //        return false;
        //    }
        //}



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
        public bool SaveInvoice(InvoiceDto invoiceDto) // define InvoiceDto class matching payload
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                using (var txn = conn.BeginTransaction())
                {
                    try
                    {
                        // 1) Insert invoice header
                        string insertHeader = @"
INSERT INTO Invoice (InvoiceNo, InvoiceNum, InvoiceDate, CompanyProfileId, CustomerId, CustomerName, CustomerPhone, CustomerAddress, SubTotal, TotalTax, TotalAmount, RoundOff, CreatedBy, CreatedAt)
VALUES (@InvoiceNo, @InvoiceNum, @InvoiceDate, @CompanyProfileId, @CustomerId, @CustomerName, @CustomerPhone, @CustomerAddress, @SubTotal, @TotalTax, @TotalAmount, @RoundOff, @CreatedBy, datetime('now','localtime'));
SELECT last_insert_rowid();
";
                        using (var cmd = new SQLiteCommand(insertHeader, conn, txn))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceNo", invoiceDto.InvoiceNo);
                            cmd.Parameters.AddWithValue("@InvoiceNum", invoiceDto.InvoiceNum);
                            cmd.Parameters.AddWithValue("@InvoiceDate", invoiceDto.InvoiceDate);
                            cmd.Parameters.AddWithValue("@CompanyProfileId", invoiceDto.CompanyProfileId);
                            cmd.Parameters.AddWithValue("@CustomerId", invoiceDto.CustomerId == 0 ? (object)DBNull.Value : invoiceDto.CustomerId);
                            cmd.Parameters.AddWithValue("@CustomerName", invoiceDto.CustomerName);
                            cmd.Parameters.AddWithValue("@CustomerPhone", invoiceDto.CustomerPhone);
                            cmd.Parameters.AddWithValue("@CustomerAddress", invoiceDto.CustomerAddress);
                            cmd.Parameters.AddWithValue("@SubTotal", invoiceDto.SubTotal);
                            cmd.Parameters.AddWithValue("@TotalTax", invoiceDto.TotalTax);
                            cmd.Parameters.AddWithValue("@TotalAmount", invoiceDto.TotalAmount);
                            cmd.Parameters.AddWithValue("@RoundOff", invoiceDto.RoundOff);
                            cmd.Parameters.AddWithValue("@CreatedBy", invoiceDto.CreatedBy);
                            long invoiceId = (long)cmd.ExecuteScalar();

                            // 2) Insert invoice items and update inventory
                            string insertItem = @"
INSERT INTO InvoiceItem (InvoiceId, ItemId, ItemName, HsnCode, BatchNo, Qty, Unit, Rate, Amount, GstPercent, TaxAmount, Cgst, Sgst, Igst, Discount)
VALUES (@InvoiceId,@ItemId,@ItemName,@HsnCode,@BatchNo,@Qty,@Unit,@Rate,@Amount,@GstPercent,@TaxAmount,@Cgst,@Sgst,@Igst,@Discount);
";
                            using (var cmdItem = new SQLiteCommand(insertItem, conn, txn))
                            {
                                foreach (var line in invoiceDto.Items)
                                {
                                    cmdItem.Parameters.Clear();
                                    cmdItem.Parameters.AddWithValue("@InvoiceId", invoiceId);
                                    cmdItem.Parameters.AddWithValue("@ItemId", line.ItemId == 0 ? (object)DBNull.Value : line.ItemId);
                                    cmdItem.Parameters.AddWithValue("@ItemName", line.ItemName);
                                    cmdItem.Parameters.AddWithValue("@HsnCode", line.HsnCode ?? "");
                                    cmdItem.Parameters.AddWithValue("@BatchNo", line.BatchNo ?? "");
                                    cmdItem.Parameters.AddWithValue("@Qty", line.Qty);
                                    cmdItem.Parameters.AddWithValue("@Unit", line.Unit ?? "");
                                    cmdItem.Parameters.AddWithValue("@Rate", line.Rate);
                                    cmdItem.Parameters.AddWithValue("@Amount", line.Amount);
                                    cmdItem.Parameters.AddWithValue("@GstPercent", line.GstPercent);
                                    cmdItem.Parameters.AddWithValue("@TaxAmount", line.TaxAmount);
                                    cmdItem.Parameters.AddWithValue("@Cgst", line.Cgst);
                                    cmdItem.Parameters.AddWithValue("@Sgst", line.Sgst);
                                    cmdItem.Parameters.AddWithValue("@Igst", line.Igst);
                                    cmdItem.Parameters.AddWithValue("@Discount", line.Discount);
                                    cmdItem.ExecuteNonQuery();

                                    //3 insert into ItemLedger
                                    string sql = @"
            INSERT INTO ItemLedger 
            (ItemId, BatchNo, Date, TxnType, RefNo, Qty, Rate, DiscountPercent, NetRate, TotalAmount, Remarks, CreatedBy)
            VALUES 
            (@ItemId, @BatchNo, @Date, @TxnType, @RefNo, @Qty, @Rate, @DiscountPercent, @NetRate ,@Amount, @Remarks, @CreatedBy);
        ";

                                    using (var cmditemledger = new SQLiteCommand(sql, conn, txn))
                                    {

                                        cmd.Parameters.AddWithValue("@ItemId", line.ItemId);
                                        cmd.Parameters.AddWithValue("@BatchNo", line.BatchNo ?? "");
                                        cmd.Parameters.AddWithValue("@Date", invoiceDto.InvoiceDate);
                                        cmd.Parameters.AddWithValue("@TxnType", "Sale");
                                        cmd.Parameters.AddWithValue("@RefNo", invoiceDto.InvoiceNum);
                                        cmd.Parameters.AddWithValue("@Qty", line.Qty);
                                        cmd.Parameters.AddWithValue("@Rate", line.Rate);
                                        cmd.Parameters.AddWithValue("@DiscountPercent", line.Discount);
                                        cmd.Parameters.AddWithValue("@NetRate", line.Amount);
                                        cmd.Parameters.AddWithValue("@Amount", line.Amount + line.TaxAmount);
                                        cmd.Parameters.AddWithValue("@Remarks", "Sale");
                                        cmd.Parameters.AddWithValue("@CreatedBy", invoiceDto.CreatedBy ?? "System");

                                        cmditemledger.ExecuteNonQuery();
                                    }
                                    // 4) Deduct inventory - implement carefully to match your ItemBalance table
                                    // Example: subtract from CurrentQtyBatchWise when batch matches, and update total CurrentQty
                                    string updateBatchQty = @"
UPDATE ItemBalance 
SET CurrentQtyBatchWise = CurrentQtyBatchWise - @Qty,
    LastUpdated = datetime('now','localtime')
WHERE ItemId = @ItemId AND BatchNo = @BatchNo;
";
                                    using (var cmdInv = new SQLiteCommand(updateBatchQty, conn, txn))
                                    {
                                        cmdInv.Parameters.AddWithValue("@Qty", line.Qty);
                                        cmdInv.Parameters.AddWithValue("@ItemId", line.ItemId);
                                        cmdInv.Parameters.AddWithValue("@BatchNo", line.BatchNo ?? "");
                                        cmdInv.ExecuteNonQuery();
                                    }

                                    // Update aggregated CurrentQty
                                    string updateTotal = @"
UPDATE ItemBalance
SET CurrentQty = (
    SELECT SUM(CurrentQtyBatchWise) FROM ItemBalance sub WHERE sub.ItemId = @ItemId
),
LastUpdated = datetime('now','localtime')
WHERE Id = (SELECT MAX(Id) FROM ItemBalance WHERE ItemId = @ItemId);
";
                                    using (var cmdTotal = new SQLiteCommand(updateTotal, conn, txn))
                                    {
                                        cmdTotal.Parameters.AddWithValue("@ItemId", line.ItemId);
                                        cmdTotal.ExecuteNonQuery();
                                    }

                                    // Optional: insert ledger entry for each line (if you maintain ledger)
                                    // db.AddLedgerEntry(...) or similar
                                }
                            }

                            txn.Commit();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("SaveInvoice failed: " + ex.Message);
                        try { txn.Rollback(); } catch { }
                        return false;
                    }
                }
       
            }
        }
        
        public InvoiceFullDto GetInvoice(int invoiceId)
        {
            var invoice = new InvoiceFullDto();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // =============================
                // 1️⃣ FETCH INVOICE HEADER
                // =============================
                string sqlHeader = @"
            SELECT 
                Id AS InvoiceNum,
                InvoiceNo,
                InvoiceDate,
                CompanyProfileId,
                CustomerId,
                CustomerName,
                CustomerPhone,
                CustomerAddress,
                SubTotal,
                TotalTax,
                TotalAmount,
                RoundOff,
                CreatedBy,
                CreatedAt
            FROM Invoice
            WHERE Id = @Id;
        ";

                using (var cmd = new SQLiteCommand(sqlHeader, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            invoice.InvoiceNum = reader.GetInt32(0);
                            invoice.InvoiceNo = reader["InvoiceNo"]?.ToString();
                            invoice.InvoiceDate = reader.GetDateTime(reader.GetOrdinal("InvoiceDate"));
                            invoice.CompanyProfileId = Convert.ToInt32(reader["CompanyProfileId"]);

                            invoice.CustomerId = reader["CustomerId"] as int?;
                            invoice.CustomerName = reader["CustomerName"]?.ToString();
                            invoice.CustomerPhone = reader["CustomerPhone"]?.ToString();
                            invoice.CustomerAddress = reader["CustomerAddress"]?.ToString();

                            invoice.SubTotal = Convert.ToDecimal(reader["SubTotal"]);
                            invoice.TotalTax = Convert.ToDecimal(reader["TotalTax"]);
                            invoice.TotalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                            invoice.RoundOff = Convert.ToDecimal(reader["RoundOff"]);

                            invoice.CreatedBy = reader["CreatedBy"]?.ToString();
                            invoice.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                // =============================
                // 2️⃣ FETCH INVOICE ITEMS
                // =============================
                string sqlItems = @"
            SELECT 
                ii.ItemId,
                i.Name AS ItemName,
                i.HsnCode,
                ii.BatchNo,
                ii.Qty,
                ii.Unit,
                ii.Rate,
                ii.Amount,
                ii.GstPercent,
                ii.TaxAmount,
                ii.Cgst,
                ii.Sgst,
                ii.Igst,
                ii.Discount
            FROM InvoiceItem ii
            LEFT JOIN Item i ON i.Id = ii.ItemId
            WHERE ii.InvoiceId = @Id;
        ";

                using (var cmd = new SQLiteCommand(sqlItems, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoiceId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            invoice.Items.Add(new InvoiceItemDto
                            {
                                ItemId = Convert.ToInt32(reader["ItemId"]),
                                ItemName = reader["ItemName"]?.ToString(),
                                HsnCode = reader["HsnCode"]?.ToString(),
                                BatchNo = reader["BatchNo"]?.ToString(),
                                Qty = Convert.ToDecimal(reader["Qty"]),
                                Unit = reader["Unit"]?.ToString(),
                                Rate = Convert.ToDecimal(reader["Rate"]),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                GstPercent = Convert.ToDecimal(reader["GstPercent"]),
                                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                                Cgst = Convert.ToDecimal(reader["Cgst"]),
                                Sgst = Convert.ToDecimal(reader["Sgst"]),
                                Igst = Convert.ToDecimal(reader["Igst"]),
                                Discount = Convert.ToDecimal(reader["Discount"])
                            });
                        }
                    }
                }

                // =============================
                // 3️⃣ FETCH COMPANY PROFILE
                // =============================
                string sqlCompany = @"
            SELECT 
                Logo,
                CompanyName,
                AddressLine1,
                AddressLine2,
                City,
                State,
                Pincode,
                Country,
                GSTIN,
                PAN,
                Email,
                Phone,
                BankName,
                BankAccount,
                IFSC,
                BranchName
            FROM CompanyProfile
            WHERE Id = @Id;
        ";

                using (var cmd = new SQLiteCommand(sqlCompany, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", invoice.CompanyProfileId);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            invoice.CompanyLogo = r["Logo"] != DBNull.Value ? (byte[])r["Logo"] : null;
                            invoice.CompanyName = r["CompanyName"]?.ToString();
                            invoice.CompanyAddressLine1 = r["AddressLine1"]?.ToString();
                            invoice.CompanyAddressLine2 = r["AddressLine2"]?.ToString();
                            invoice.CompanyCity = r["City"]?.ToString();
                            invoice.CompanyState = r["State"]?.ToString();
                            invoice.CompanyPincode = r["Pincode"]?.ToString();
                            invoice.CompanyCountry = r["Country"]?.ToString();
                            invoice.CompanyGstin = r["GSTIN"]?.ToString();
                            invoice.CompanyPan = r["PAN"]?.ToString();
                            invoice.CompanyEmail = r["Email"]?.ToString();
                            invoice.CompanyPhone = r["Phone"]?.ToString();
                            invoice.CompanyBankName = r["BankName"]?.ToString();
                            invoice.CompanyBankAccount = r["BankAccount"]?.ToString();
                            invoice.CompanyIfsc = r["IFSC"]?.ToString();
                            invoice.CompanyBranchName = r["BranchName"]?.ToString();
                        }
                    }
                }
            }

            return invoice;
            
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
INSERT INTO Customers (Name, Phone, Address)
VALUES (@Name, @Phone, @Address);
SELECT last_insert_rowid();
";

                using (var cmd = new SQLiteCommand(sqlInsert, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", c.Name ?? "");
                    cmd.Parameters.AddWithValue("@Phone", c.Phone ?? "");
                    cmd.Parameters.AddWithValue("@Address", c.Address ?? "");
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

    }

}
