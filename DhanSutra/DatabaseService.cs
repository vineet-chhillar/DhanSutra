using Dapper;
using DhanSutra.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DhanSutra
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=billing.db;Version=3;";

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

        public void AddItemDetails(ItemDetails details)
        {
            // 🧾 Log what we are actually inserting
            Console.WriteLine("📥 AddItem() received:");
            Console.WriteLine(JsonConvert.SerializeObject(details, Formatting.Indented));
            using (var connection = new SQLiteConnection(_connectionString))
                connection.Execute(@"
            INSERT INTO ItemDetails (item_id, hsnCode, batchNo,[date], quantity, purchasePrice, salesPrice, mrp, goodsOrServices, description, mfgdate, expdate, modelno, brand, size, color, weight, dimension,createdby,createdat)
                           VALUES (@Item_Id, @HsnCode, @BatchNo,@Date, @Quantity, @PurchasePrice, @SalesPrice, @Mrp, @GoodsOrServices, @Description, @MfgDate, @ExpDate, @ModelNo, @Brand, @Size, @Color, @Weight, @Dimension,@CreatedBy,@CreatedAt)", details);
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
                c.categoryname AS CategoryName,
                u.UnitName AS UnitName,
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
                                CategoryName = reader["CategoryName"],
                                UnitName = reader["UnitName"],
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
                    Date,
                    Quantity,
                    PurchasePrice,
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
                    IFNULL(HsnCode, '') LIKE @query OR
                    IFNULL(BatchNo, '') LIKE @query OR
                    IFNULL(Description, '') LIKE @query OR
                    IFNULL(Brand, '') LIKE @query OR
                    IFNULL(ModelNo, '') LIKE @query OR
                    CAST(Item_Id AS TEXT) LIKE @query
                ORDER BY Date DESC;";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@query", $"%{queryText}%");

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
        //public DbResult UpdateInventory(JObject payload)
        //{
        //    try
        //    {
        //        int id = payload["id"]?.Value<int>() ?? 0;
        //        if (id == 0)
        //            return new DbResult { Success = false, Message = "Invalid inventory ID." };

        //        using var conn = new SQLiteConnection(_connectionString);
        //        conn.Open();
        //        using var tx = conn.BeginTransaction();

        //        string sql = @"
        //            UPDATE Inventory SET
        //                Item_Id = @Item_Id,
        //                HsnCode = @HsnCode,
        //                BatchNo = @BatchNo,
        //                Date = @Date,
        //                Quantity = @Quantity,
        //                PurchasePrice = @PurchasePrice,
        //                SalesPrice = @SalesPrice,
        //                Mrp = @Mrp,
        //                GoodsOrServices = @GoodsOrServices,
        //                Description = @Description,
        //                MfgDate = @MfgDate,
        //                ExpDate = @ExpDate,
        //                ModelNo = @ModelNo,
        //                Brand = @Brand,
        //                Size = @Size,
        //                Color = @Color,
        //                Weight = @Weight,
        //                Dimension = @Dimension
        //            WHERE Id = @Id;";

        //        using var cmd = new SQLiteCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@Item_Id", payload["item_id"]?.Value<int>() ?? 0);
        //        cmd.Parameters.AddWithValue("@HsnCode", payload["hsnCode"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@BatchNo", payload["batchNo"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Date", payload["date"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Quantity", payload["quantity"]?.Value<int>() ?? 0);
        //        cmd.Parameters.AddWithValue("@PurchasePrice", payload["purchasePrice"]?.Value<double>() ?? 0);
        //        cmd.Parameters.AddWithValue("@SalesPrice", payload["salesPrice"]?.Value<double>() ?? 0);
        //        cmd.Parameters.AddWithValue("@Mrp", payload["mrp"]?.Value<double>() ?? 0);
        //        cmd.Parameters.AddWithValue("@GoodsOrServices", payload["goodsOrServices"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Description", payload["description"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@MfgDate", payload["mfgDate"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@ExpDate", payload["expDate"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@ModelNo", payload["modelNo"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Brand", payload["brand"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Size", payload["size"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Color", payload["color"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Weight", payload["weight"]?.Value<double>() ?? 0);
        //        cmd.Parameters.AddWithValue("@Dimension", payload["dimension"]?.ToString() ?? "");
        //        cmd.Parameters.AddWithValue("@Id", id);

        //        int rows = cmd.ExecuteNonQuery();
        //        tx.Commit();

        //        return new DbResult
        //        {
        //            Success = rows > 0,
        //            Message = rows > 0
        //                ? "✅ Inventory record updated successfully!"
        //                : "⚠️ No matching record found."
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Update error: {ex.Message}");
        //        return new DbResult
        //        {
        //            Success = false,
        //            Message = $"Update failed: {ex.Message}"
        //        };
        //    }
        //}

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

    }

}
