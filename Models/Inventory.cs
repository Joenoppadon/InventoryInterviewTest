using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        public int ProductId { get; set; }
        public int Quantity { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}