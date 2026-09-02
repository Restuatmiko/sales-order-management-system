using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesOrderService.Data;
using SalesOrderService.Models;
using SalesOrderService.DTOs;
using Microsoft.EntityFrameworkCore.Storage;
using ClosedXML.Excel;

namespace SalesOrderService.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SalesOrders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderResponse>>> GetSalesOrders(
    [FromQuery] string? keyword,
    [FromQuery] DateTime? orderDate)
        {
            var query = _context.SalesOrders
                .Include(s => s.Items)
                .AsQueryable();

            // Filter keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(s =>
                    s.SO_NO.Contains(keyword) ||
                    s.ADDRESS.Contains(keyword) ||
                    _context.Customers.Any(c =>
                        c.COM_CUSTOMER_ID == s.COM_CUSTOMER_ID &&
                        c.CUSTOMER_NAME.Contains(keyword)
                    )
                );
            }

            // Filter tanggal
            if (orderDate.HasValue)
            {
                query = query.Where(s =>
                    s.ORDER_DATE.Date == orderDate.Value.Date);
            }

            var salesOrders = await query.ToListAsync();

            var result = new List<SalesOrderResponse>();

            foreach (var salesOrder in salesOrders)
            {
                var customerName = await _context.Customers
                    .Where(c => c.COM_CUSTOMER_ID == salesOrder.COM_CUSTOMER_ID)
                    .Select(c => c.CUSTOMER_NAME)
                    .FirstOrDefaultAsync();

                result.Add(new SalesOrderResponse
                {
                    SalesSoId = salesOrder.SALES_SO_ID,
                    SoNo = salesOrder.SO_NO,
                    OrderDate = salesOrder.ORDER_DATE,
                    CustomerId = salesOrder.COM_CUSTOMER_ID,
                    CustomerName = customerName ?? string.Empty,
                    Address = salesOrder.ADDRESS,

                    GrandTotal = salesOrder.Items.Sum(i =>
                        i.QUANTITY * i.PRICE),

                    Items = salesOrder.Items.Select(i => new SalesOrderItemResponse
                    {
                        SalesOrderLineItemId = i.SALES_SO_LITEM_ID,
                        ItemName = i.ITEM_NAME,
                        Quantity = i.QUANTITY,
                        Price = i.PRICE,
                        Total = i.QUANTITY * i.PRICE
                    }).ToList()
                });
            }

            return Ok(result);
        }
        // GET: api/SalesOrders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesOrderResponse>> GetSalesOrder(int id)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SALES_SO_ID == id);

            if (salesOrder == null)
            {
                return NotFound("Order tidak ditemukan.");
            }

            var result = new SalesOrderResponse
            {
                SalesSoId = salesOrder.SALES_SO_ID,
                SoNo = salesOrder.SO_NO,
                OrderDate = salesOrder.ORDER_DATE,
                CustomerId = salesOrder.COM_CUSTOMER_ID,

                CustomerName = await _context.Customers
                    .Where(c => c.COM_CUSTOMER_ID == salesOrder.COM_CUSTOMER_ID)
                    .Select(c => c.CUSTOMER_NAME)
                    .FirstOrDefaultAsync() ?? string.Empty,

                Address = salesOrder.ADDRESS,

                GrandTotal = salesOrder.Items.Sum(i =>
                    i.QUANTITY * i.PRICE),

                Items = salesOrder.Items.Select(i => new SalesOrderItemResponse
                {
                    SalesOrderLineItemId = i.SALES_SO_LITEM_ID,
                    ItemName = i.ITEM_NAME,
                    Quantity = i.QUANTITY,
                    Price = i.PRICE,
                    Total = i.QUANTITY * i.PRICE
                }).ToList()
            };

            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateSalesOrder(CreateSalesOrderRequest request)
        {
            // Validasi SO No
            if (string.IsNullOrWhiteSpace(request.SO_NO))
            {
                return BadRequestError("SO No tidak boleh kosong.");
            }

            // Cek SO No duplikat
            var existingSO = await _context.SalesOrders
                .AnyAsync(s => s.SO_NO == request.SO_NO);

            if (existingSO)
            {
                return ConflictError("SO No sudah digunakan.");
            }

            // Validasi Order Date
            if (!request.ORDER_DATE.HasValue)
            {
                return BadRequestError("Order Date tidak boleh kosong.");
            }

            // Validasi Customer
            if (!request.COM_CUSTOMER_ID.HasValue)
            {
                return BadRequestError("Customer harus dipilih.");
            }

            var customerExists = await _context.Customers
                .AnyAsync(c => c.COM_CUSTOMER_ID == request.COM_CUSTOMER_ID.Value);

            if (!customerExists)
            {
                return NotFoundError("Customer tidak ditemukan.");
            }

            // Validasi Items
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequestError("Minimal harus ada 1 item.");
            }

            // Validasi setiap item
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ITEM_NAME))
                {
                    return BadRequestError("Nama item tidak boleh kosong.");
                }

                if (item.QUANTITY <= 0)
                {
                    return BadRequestError("Quantity harus lebih dari 0.");
                }

                if (item.PRICE <= 0)
                {
                    return BadRequestError("Price harus lebih dari 0.");
                }
            }

            // Mulai transaksi
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Buat Sales Order Header
                var salesOrder = new SalesOrder
                {
                    SO_NO = request.SO_NO,
                    ORDER_DATE = request.ORDER_DATE.Value,
                    COM_CUSTOMER_ID = request.COM_CUSTOMER_ID.Value,
                    ADDRESS = request.ADDRESS
                };

                _context.SalesOrders.Add(salesOrder);

                // Simpan header
                await _context.SaveChangesAsync();

                // Simpan item
                foreach (var item in request.Items)
                {
                    var salesOrderItem = new SalesOrderLineItem
                    {
                        SALES_SO_ID = salesOrder.SALES_SO_ID,
                        ITEM_NAME = item.ITEM_NAME,
                        QUANTITY = item.QUANTITY,
                        PRICE = item.PRICE
                    };

                    _context.SalesOrderLineItems.Add(salesOrderItem);
                }

                await _context.SaveChangesAsync();

                // Commit
                await transaction.CommitAsync();
                // Hitung Grand Total di Sales Order Service
                var grandTotal = request.Items.Sum(item =>
                    item.QUANTITY * item.PRICE);
                return StatusCode(201, new
                {
                    success = true,
                    salesSoId = salesOrder.SALES_SO_ID,
                    message = "Order berhasil dibuat"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                              ?? ex.InnerException?.Message
                              ?? ex.Message
                });
            }
        }
        // PUT: api/orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSalesOrder(
            int id,
            [FromBody] UpdateSalesOrderRequest request)
        {
            // Cari Sales Order
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SALES_SO_ID == id);

            if (salesOrder == null)
            {
                return NotFoundError("Sales Order tidak ditemukan.");
            }

            // Validasi SO No
            if (string.IsNullOrWhiteSpace(request.SO_NO))
            {
                return BadRequestError("SO No tidak boleh kosong.");
            }

            // Cek SO No duplikat
            var existingSO = await _context.SalesOrders
                .AnyAsync(s =>
                    s.SO_NO == request.SO_NO &&
                    s.SALES_SO_ID != id);

            if (existingSO)
            {
                return ConflictError("SO No sudah digunakan.");
            }

            // Validasi Order Date
            if (!request.ORDER_DATE.HasValue)
            {
                return BadRequestError("Order Date tidak boleh kosong.");
            }

            // Validasi Customer
            if (!request.COM_CUSTOMER_ID.HasValue)
            {
                return BadRequestError("Customer harus dipilih.");
            }

            var customerExists = await _context.Customers
                .AnyAsync(c =>
                    c.COM_CUSTOMER_ID == request.COM_CUSTOMER_ID.Value);

            if (!customerExists)
            {
                return NotFoundError("Customer tidak ditemukan.");
            }

            // Validasi Items
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequestError("Minimal harus ada 1 item.");
            }

            // Validasi setiap item
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ITEM_NAME))
                {
                    return BadRequestError("Nama item tidak boleh kosong.");
                }

                if (item.QUANTITY <= 0)
                {
                    return BadRequestError("Quantity harus lebih dari 0.");
                }

                if (item.PRICE <= 0)
                {
                    return BadRequestError("Price harus lebih dari 0.");
                }
            }

            // Mulai transaksi
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Update Header
                salesOrder.SO_NO = request.SO_NO;
                salesOrder.ORDER_DATE = request.ORDER_DATE.Value;
                salesOrder.COM_CUSTOMER_ID = request.COM_CUSTOMER_ID.Value;
                salesOrder.ADDRESS = request.ADDRESS;

                // Hapus item lama
                _context.SalesOrderLineItems.RemoveRange(salesOrder.Items);

                // Tambahkan item baru
                foreach (var item in request.Items)
                {
                    var salesOrderItem = new SalesOrderLineItem
                    {
                        SALES_SO_ID = salesOrder.SALES_SO_ID,
                        ITEM_NAME = item.ITEM_NAME,
                        QUANTITY = item.QUANTITY,
                        PRICE = item.PRICE
                    };

                    _context.SalesOrderLineItems.Add(salesOrderItem);
                }

                await _context.SaveChangesAsync();

                // Commit transaksi
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    salesSoId = salesOrder.SALES_SO_ID,
                    message = "Order berhasil diupdate"
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = "Gagal mengupdate order"
                });
            }
        }

        // DELETE: api/orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSalesOrder(int id)
        {
            // Cari Sales Order
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SALES_SO_ID == id);

            if (salesOrder == null)
            {
                return NotFoundError("Sales Order tidak ditemukan.");
            }

            // Mulai transaksi
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Hapus Sales Order
                // Item akan ikut terhapus karena Cascade Delete
                _context.SalesOrders.Remove(salesOrder);

                await _context.SaveChangesAsync();

                // Commit
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    salesSoId = id,
                    message = "Order berhasil dihapus"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                              ?? ex.InnerException?.Message
                              ?? ex.Message
                });
            }
        }
        // GET: api/orders/export
        [HttpGet("export")]
        public async Task<IActionResult> ExportSalesOrders(
            [FromQuery] string? keyword,
            [FromQuery] DateTime? orderDate)
        {
            var query = _context.SalesOrders
                .Include(s => s.Items)
                .AsQueryable();

            // Filter keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(s =>
                    s.SO_NO.Contains(keyword) ||
                    s.ADDRESS.Contains(keyword) ||
                    _context.Customers.Any(c =>
                        c.COM_CUSTOMER_ID == s.COM_CUSTOMER_ID &&
                        c.CUSTOMER_NAME.Contains(keyword)
                    )
                );
            }

            // Filter tanggal
            if (orderDate.HasValue)
            {
                query = query.Where(s =>
                    s.ORDER_DATE.Date == orderDate.Value.Date);
            }

            var salesOrders = await query
                .OrderBy(s => s.ORDER_DATE)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Sales Orders");

            // Header Excel
            worksheet.Cell(1, 1).Value = "SO Number";
            worksheet.Cell(1, 2).Value = "Order Date";
            worksheet.Cell(1, 3).Value = "Customer Name";
            worksheet.Cell(1, 4).Value = "Address";

            // Style header
            worksheet.Range(1, 1, 1, 4).Style.Font.Bold = true;

            int row = 2;

            foreach (var salesOrder in salesOrders)
            {
                var customerName = await _context.Customers
                    .Where(c =>
                        c.COM_CUSTOMER_ID == salesOrder.COM_CUSTOMER_ID)
                    .Select(c => c.CUSTOMER_NAME)
                    .FirstOrDefaultAsync();

                worksheet.Cell(row, 1).Value = salesOrder.SO_NO;
                worksheet.Cell(row, 2).Value = salesOrder.ORDER_DATE;
                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(row, 3).Value = customerName ?? string.Empty;
                worksheet.Cell(row, 4).Value = salesOrder.ADDRESS;

                row++;
            }

            // Sesuaikan lebar kolom
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            var fileName =
                $"SalesOrder_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        // POST: api/orders/calculate
        [HttpPost("calculate")]
        public IActionResult CalculateSalesOrder(
            [FromBody] CreateSalesOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return Ok(new
                {
                    grandTotal = 0
                });
            }

            foreach (var item in request.Items)
            {
                if (item.QUANTITY <= 0 || item.PRICE <= 0)
                {
                    return BadRequestError("Quantity dan Price harus lebih dari 0.");
                }
            }

            var grandTotal = request.Items.Sum(item =>
                item.QUANTITY * item.PRICE);

            return Ok(new
            {
                grandTotal = grandTotal
            });
        }
        [HttpPost("calculate-item")]
        public IActionResult CalculateItem(
    [FromBody] CreateSalesOrderItemRequest item)
        {
            if (string.IsNullOrWhiteSpace(item.ITEM_NAME))
            {
                return BadRequestError("Item Name wajib diisi.");
            }

            if (item.QUANTITY <= 0)
            {
                return BadRequestError("Quantity harus lebih dari 0.");
            }

            if (item.PRICE <= 0)
            {
                return BadRequestError("Price harus lebih dari 0.");
            }

            var total = item.QUANTITY * item.PRICE;

            return Ok(new
            {
                itemName = item.ITEM_NAME,
                quantity = item.QUANTITY,
                price = item.PRICE,
                total = total
            });
        }
        private IActionResult BadRequestError(string message)
        {
            return BadRequest(new
            {
                success = false,
                message = message,
                errors = new[] { message }
            });
        }

        private IActionResult NotFoundError(string message)
        {
            return NotFound(new
            {
                success = false,
                message = message,
                errors = new[] { message }
            });
        }

        private IActionResult ConflictError(string message)
        {
            return Conflict(new
            {
                success = false,
                message = message,
                errors = new[] { message }
            });
        }
    }
}