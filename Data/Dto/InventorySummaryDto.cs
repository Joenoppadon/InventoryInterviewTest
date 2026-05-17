using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Data.Dto
{
    public class InventorySummaryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        public int CurrentQuantity { get; set; }
        public string Location { get; set; }
    }
}