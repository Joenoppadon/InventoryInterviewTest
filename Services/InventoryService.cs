using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryTestMvc.Data;
using InventoryTestMvc.Data.Dto;
using InventoryTestMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTestMvc.Services
{

    public class InventoryService
    {
        private readonly InventoryTestMvcDbContext _context;

        public InventoryService(InventoryTestMvcDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ฟังก์ชัน: ดึงรายการสินค้าคงเหลือในคลัง (หน้า Index)
        // ==========================================
        public async Task<List<InventorySummaryDto>> GetInventorySummaryAsync()
        {
            return await _context.Inventories
                .Include(i => i.Product) // Join ตาราง Products เพื่อเอาชื่อและ SKU
                .Select(i => new InventorySummaryDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    SKU = i.Product.SKU,
                    CurrentQuantity = i.Quantity,
                    Location = i.Location
                })
                .ToListAsync();
        }

        // ==========================================
        // 2. ฟังก์ชันเสริม: ลงทะเบียนสินค้าใหม่ (Master Product)
        // ==========================================
        public async Task CreateProductAsync(CreateProductDto dto)
        {
            // ตรวจสอบความซ้ำซ้อนของ SKU ก่อนบันทึก
            var isSkuExists = await _context.Products.AnyAsync(p => p.SKU == dto.SKU);
            if (isSkuExists)
            {
                throw new InvalidOperationException($"รหัส SKU: {dto.SKU} นี้มีอยู่ในระบบแล้ว ไม่สามารถบันทึกซ้ำได้");
            }

            var product = new Product
            {
                ProductName = dto.ProductName,
                SKU = dto.SKU,
                UnitPrice = dto.UnitPrice,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // 3. ฟังก์ชัน: รับสินค้าเข้าคลัง (Receive / PO)
        // ==========================================
        public async Task ReceiveProductAsync(ReceiveProductDto dto)
        {
            // ดักข้อผิดพลาด Foreign Key: ตรวจสอบก่อนว่ามีรหัสสินค้านี้จริงในระบบไหม
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists)
            {
                throw new KeyNotFoundException($"ไม่พบรหัสสินค้า (Product ID: {dto.ProductId}) ในฐานข้อมูล กรุณาลงทะเบียนสินค้าใหม่ก่อนทำรายการ");
            }

            //ตรวจสอบบันทึกซ้ำ
            var poExists = await _context.PurchaseOrders.Join(_context.StockTransactions,
                p => p.POID,
                s => s.RefID,
                (p, s) => new { p.PONumber, s.ProductId }
                ).AnyAsync(f => f.PONumber == dto.PONumber && f.ProductId == dto.ProductId);
            if (poExists)
            {
                throw new Exception($"ข้อมูลซ้ำกันในระบบ");
            }

            // เปิดใช้งาน Transaction เพื่อความปลอดภัยของข้อมูลคลังสินค้า
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. ตรวจสอบหรือสร้างเอกสารใบสั่งซื้อ (PurchaseOrder)
                var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.PONumber == dto.PONumber);
                if (po == null)
                {
                    po = new PurchaseOrder
                    {
                        PONumber = dto.PONumber,
                        SupplierName = dto.SupplierName,
                        OrderDate = DateTime.Now,
                        TotalAmount = dto.Quantity * dto.UnitPrice
                    };
                    _context.PurchaseOrders.Add(po);
                    await _context.SaveChangesAsync(); // เซฟชั่วคราวเพื่อเอา POID ไปผูกกับ Transaction ข้อมูลการโยกย้าย
                }
                else
                {
                    // ถ้าใบ PO เดิมมีอยู่แล้ว ให้บวกเพิ่มยอดเงินสะสมเข้าไป
                    po.TotalAmount += (dto.Quantity * dto.UnitPrice);
                }

                // 2. บันทึกประวัติการรับเข้าลงตารางกลาง (StockTransaction - ขาเข้า 'IN')
                var stockTrans = new StockTransaction
                {
                    RefID = po.POID,
                    RefType = "IN",
                    ProductId = dto.ProductId,
                    Qty = dto.Quantity
                };
                _context.StockTransactions.Add(stockTrans);

                // 3. จัดการอัปเดตยอดคงเหลือในคลังสินค้า (Inventories)
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);
                if (inventory == null)
                {
                    // หากยังไม่เคยมีสินค้านี้ในคลังเลย ให้เปิด Record ใหม่ในคลัง
                    inventory = new Inventory
                    {
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        Location = "Main Warehouse",
                        LastUpdated = DateTime.Now
                    };
                    _context.Inventories.Add(inventory);
                }
                else
                {
                    // หากมีรายการอยู่แล้ว ให้บวกจำนวนเพิ่มเข้าไปในสต็อกเดิม
                    inventory.Quantity += dto.Quantity;
                    inventory.LastUpdated = DateTime.Now;
                }

                // บันทึกการเปลี่ยนแปลงทั้งหมดลง Database พร้อมกัน
                await _context.SaveChangesAsync();

                // ยืนยันความถูกต้อง ถ้าระบบทำงานผ่านฉลุยทุกบรรทัด ข้อมูลจะถูกบันทึกจริง
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // หากเกิด Error ขึ้นระหว่างทาง คืนค่าเดิมทั้งหมด (Rollback) ข้อมูลจะไม่พังเสียหาย
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==========================================
        // 4. ฟังก์ชัน: ส่งสินค้าออก / ตัดสต็อก (Delivery)
        // ==========================================
        public async Task DeliverProductAsync(DeliverProductDto dto)
        {
            // ดักข้อผิดพลาด: ตรวจสอบสต็อกปัจจุบันในคลังก่อนว่าพอให้ตัดยอดออกไหม
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);
            if (inventory == null || inventory.Quantity < dto.Quantity)
            {
                throw new InvalidOperationException($"สินค้าในคลังไม่พอส่ง! ปัจจุบันคงเหลือในคลังเพียง {inventory?.Quantity ?? 0} ชิ้น");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. ตรวจสอบหรือสร้างเอกสารใบส่งของ (Delivery)
                var delivery = await _context.Deliveries.FirstOrDefaultAsync(d => d.DeliveryNumber == dto.DeliveryNumber);
                if (delivery == null)
                {
                    delivery = new Delivery
                    {
                        DeliveryNumber = dto.DeliveryNumber,
                        CustomerName = dto.CustomerName,
                        DeliveryDate = DateTime.Now,
                        Status = "Shipped"
                    };
                    _context.Deliveries.Add(delivery);
                    await _context.SaveChangesAsync(); // เซฟชั่วคราวเพื่อดึง DeliveryID มาใช้งานต่อ
                }

                // 2. บันทึกประวัติการส่งออกลงตารางกลาง (StockTransaction - ขาออก 'OUT')
                var stockTrans = new StockTransaction
                {
                    RefID = delivery.DeliveryID,
                    RefType = "OUT",
                    ProductId = dto.ProductId,
                    Qty = dto.Quantity
                };
                _context.StockTransactions.Add(stockTrans);

                // 3. หักลบยอดสินค้าคงเหลือออกจากสต็อก
                inventory.Quantity -= dto.Quantity;
                inventory.LastUpdated = DateTime.Now;

                // บันทึกการเปลี่ยนแปลงทั้งหมด
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        // แสดง product ทั้งหมด
        public async Task<List<ProductListDto>> GetAllProductsAsync()
        {
            return await _context.Products
                .Select(p => new ProductListDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    SKU = p.SKU,
                    UnitPrice = p.UnitPrice,
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.ProductId) // เอาสินค้าใหม่ขึ้นก่อน
                .ToListAsync();
        }

        // report 
        public async Task<List<StockReportDto>> GetStockMovementReportAsync()
        {
            // ดึงข้อมูลธุรกรรมทั้งหมดออกมาก่อน
            var transactions = await _context.StockTransactions
                .Include(t => t.Product)
                .OrderByDescending(t => t.TransID) // เอาประวัติล่าสุดขึ้นก่อน
                .ToListAsync();

            var reportList = new List<StockReportDto>();

            foreach (var trans in transactions)
            {
                string docNumber = "-";
                DateTime docDate = DateTime.Now;

                // แยกเช็คตามประเภทธุรกรรมเพื่อไปดึงเลขที่เอกสารและวันที่ให้ถูกต้อง
                if (trans.RefType == "IN")
                {
                    var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.POID == trans.RefID);
                    docNumber = po?.PONumber ?? "ไม่พบเลขที่ PO";
                    docDate = po?.OrderDate ?? DateTime.Now;
                }
                else if (trans.RefType == "OUT")
                {
                    var delivery = await _context.Deliveries.FirstOrDefaultAsync(d => d.DeliveryID == trans.RefID);
                    docNumber = delivery?.DeliveryNumber ?? "ไม่พบเลขที่ Delivery";
                    docDate = delivery?.DeliveryDate ?? DateTime.Now;
                }

                reportList.Add(new StockReportDto
                {
                    TransID = trans.TransID,
                    RefID = trans.RefID,
                    DocumentNumber = docNumber,
                    RefType = trans.RefType,
                    ProductId = trans.ProductId,
                    ProductName = trans.Product?.ProductName ?? "ไม่พบข้อมูลสินค้า",
                    SKU = trans.Product?.SKU ?? "-",
                    Qty = trans.Qty,
                    TransactionDate = docDate
                });
            }

            return reportList;
        }

    }

}
