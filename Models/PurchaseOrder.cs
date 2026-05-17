using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int POID { get; set; }

        [Required, StringLength(50)]
        public string PONumber { get; set; }

        [StringLength(200)]
        public string SupplierName { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }
    }
}