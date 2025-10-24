using Dapper;
using DhanSutra.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra
{
    public  class DatabaseService
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


        public void DeleteItem(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Execute("DELETE FROM Item WHERE id = @Id", new { Id = id });
            }
        }
    }
    
}
