using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Models
{
    public class Delivery
    {
        [Key]
        public int DeliveryID { get; set; }

        [Required, StringLength(50)]
        public string DeliveryNumber { get; set; }

        [StringLength(200)]
        public string CustomerName { get; set; }

        public DateTime DeliveryDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; }
    }
}