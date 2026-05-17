using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InventoryTestMvc.Models;
using InventoryTestMvc.Services;
using InventoryTestMvc.Data.Dto;

namespace InventoryTestMvc.Controllers;

public class HomeController : Controller
{
    private readonly InventoryService _inventoryService;

    public HomeController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    // ==========================================
    // 1. หน้าแรก: แสดงรายการสินค้าและยอดคงเหลือในคลัง
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var inventoryList = await _inventoryService.GetInventorySummaryAsync();
            return View(inventoryList); // ส่ง List<InventorySummaryDto> ไปที่หน้าจอหลัก
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"ไม่สามารถดึงข้อมูลคลังสินค้าได้: {ex.Message}";
            return View(new List<InventorySummaryDto>());
        }
    }

    // ==========================================
    // 2. หน้าฟังก์ชันเสริม: ลงทะเบียนสินค้าใหม่ (Master Product)
    // ==========================================
    [HttpGet]
    public IActionResult CreateProduct()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductDto dto)
    {
        // ตรวจสอบ Validation เบื้องต้นที่กำหนดไว้ใน DTO
        if (!ModelState.IsValid) return View(dto);

        try
        {
            await _inventoryService.CreateProductAsync(dto);
            TempData["SuccessMessage"] = $"ลงทะเบียนสินค้า SKU: {dto.SKU} สำเร็จเรียบร้อยแล้ว!";
            return RedirectToAction(nameof(Products));
        }
        catch (InvalidOperationException ex)
        {
            // ดักจับกรณีที่ตั้ง SKU ซ้ำกันในระบบ
            ModelState.AddModelError("SKU", ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"เกิดข้อผิดพลาดในการบันทึกข้อมูลสินค้า: {ex.Message}");
            return View(dto);
        }
    }

    // ==========================================
    // 3. หน้าฟังก์ชัน: รับสินค้าเข้าคลัง (Receive / PO)
    // ==========================================
    [HttpGet]
    public IActionResult Receive()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(ReceiveProductDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        try
        {
            await _inventoryService.ReceiveProductAsync(dto);
            TempData["SuccessMessage"] = $"บันทึกใบสั่งซื้อ {dto.PONumber} และนำสินค้าเข้าคลังสำเร็จ!";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException ex)
        {
            // แก้ปัญหาขัดแย้งของ Foreign Key: ดักจับกรณีกรอกรหัสสินค้าที่ไม่เคยมีอยู่ในระบบจริง
            ModelState.AddModelError("ProductId", ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"เกิดข้อผิดพลาดในระบบขณะรับสินค้า: {ex.Message}");
            return View(dto);
        }
    }

    // ==========================================
    // 4. หน้าฟังก์ชัน: ส่งสินค้าออก / ตัดสต็อก (Delivery)
    // ==========================================
    [HttpGet]
    public IActionResult Deliver()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(DeliverProductDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        try
        {
            await _inventoryService.DeliverProductAsync(dto);
            TempData["SuccessMessage"] = $"บันทึกใบส่งของ {dto.DeliveryNumber} และตัดยอดสต็อกเรียบร้อย!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            // ดักจับกรณีที่ลูกค้าสั่งซื้อสินค้ามากกว่าจำนวนที่มีอยู่จริงในคลังสินค้า
            ModelState.AddModelError("Quantity", ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"เกิดข้อผิดพลาดในระบบขณะส่งออกสินค้า: {ex.Message}");
            return View(dto);
        }
    }

    //แสดง product ทั้งหมด

    [HttpGet]
    public async Task<IActionResult> Products()
    {
        try
        {
            var productList = await _inventoryService.GetAllProductsAsync();
            return View(productList);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"ไม่สามารถดึงข้อมูลรายการสินค้าได้: {ex.Message}";
            return View(new List<ProductListDto>());
        }
    }

    //report
    [HttpGet]
    public async Task<IActionResult> Report()
    {
        try
        {
            var reportData = await _inventoryService.GetStockMovementReportAsync();
            return View(reportData); // ส่ง List<StockReportDto> ไปที่หน้า View
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"ไม่สามารถดึงข้อมูลรายงานได้: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

}
