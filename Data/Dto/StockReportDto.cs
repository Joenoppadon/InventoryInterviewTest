using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryTestMvc.Data.Dto
{
    public class StockReportDto
    {
        public int TransID { get; set; }
        public int RefID { get; set; }          // รหัส POID หรือ DeliveryID
        public string DocumentNumber { get; set; } // เลขที่ใบ PO หรือ ใบ Delivery
        public string RefType { get; set; }      // 'IN' (นำเข้า) หรือ 'OUT' (ส่งออก)
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        public int Qty { get; set; }
        public DateTime TransactionDate { get; set; } // วันที่ทำรายการ
    }
}