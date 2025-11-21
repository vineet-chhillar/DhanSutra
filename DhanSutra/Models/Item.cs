using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DhanSutra.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ItemCode { get; set; }
        public int CategoryId { get; set; }
        public String CategoryName { get; set; }


        public DateTime Date { get; set; }
        public string Description { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public int GstId { get; set; }
        public string GstPercent { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        
    }
}
