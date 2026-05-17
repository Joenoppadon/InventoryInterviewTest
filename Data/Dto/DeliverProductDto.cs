using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Data.Dto
{
    public class DeliverProductDto
    {
        public string DeliveryNumber { get; set; }
        public string CustomerName { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}