using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Models
{
    public class StockTransaction
    {
        [Key]
        public int TransID { get; set; }

        public int RefID { get; set; } // POID หรือ DeliveryID

        [Required, StringLength(10)]
        public string RefType { get; set; } // 'IN' หรือ 'OUT'

        public int ProductId { get; set; }
        public int Qty { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}