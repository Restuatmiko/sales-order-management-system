using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesOrderService.Data;
using SalesOrderService.Models;

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
        public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrders()
        {
            var salesOrders = await _context.SalesOrders
                .Include(s => s.Items)
                .ToListAsync();

            return Ok(salesOrders);
        }
        // GET: api/SalesOrders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesOrder>> GetSalesOrder(int id)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SALES_SO_ID == id);

            if (salesOrder == null)
            {
                return NotFound();
            }

            return Ok(salesOrder);
        }
        [HttpPost]
        public async Task<IActionResult> CreateSalesOrder(CreateSalesOrderRequest request)
        {
            // Validasi SO No
            if (string.IsNullOrWhiteSpace(request.SO_NO))
            {
                return BadRequest("SO No tidak boleh kosong.");
            }

            // Cek SO No duplikat
            var existingSO = await _context.SalesOrders
                .AnyAsync(s => s.SO_NO == request.SO_NO);

            if (existingSO)
            {
                return Conflict("SO No sudah digunakan.");
            }

            // Validasi Order Date
            if (!request.ORDER_DATE.HasValue)
            {
                return BadRequest("Order Date tidak boleh kosong.");
            }

            // Validasi Customer
            if (!request.COM_CUSTOMER_ID.HasValue)
            {
                return BadRequest("Customer harus dipilih.");
            }

            var customerExists = await _context.Customers
                .AnyAsync(c => c.COM_CUSTOMER_ID == request.COM_CUSTOMER_ID.Value);

            if (!customerExists)
            {
                return NotFound("Customer tidak ditemukan.");
            }

            // Validasi Items
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("Minimal harus ada 1 item.");
            }

            // Validasi setiap item
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ITEM_NAME))
                {
                    return BadRequest("Nama item tidak boleh kosong.");
                }

                if (item.QUANTITY <= 0)
                {
                    return BadRequest("Quantity harus lebih dari 0.");
                }

                if (item.PRICE <= 0)
                {
                    return BadRequest("Price harus lebih dari 0.");
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
                return NotFound("Sales Order tidak ditemukan.");
            }

            // Validasi SO No
            if (string.IsNullOrWhiteSpace(request.SO_NO))
            {
                return BadRequest("SO No tidak boleh kosong.");
            }

            // Cek SO No duplikat
            var existingSO = await _context.SalesOrders
                .AnyAsync(s =>
                    s.SO_NO == request.SO_NO &&
                    s.SALES_SO_ID != id);

            if (existingSO)
            {
                return Conflict("SO No sudah digunakan.");
            }

            // Validasi Order Date
            if (!request.ORDER_DATE.HasValue)
            {
                return BadRequest("Order Date tidak boleh kosong.");
            }

            // Validasi Customer
            if (!request.COM_CUSTOMER_ID.HasValue)
            {
                return BadRequest("Customer harus dipilih.");
            }

            var customerExists = await _context.Customers
                .AnyAsync(c =>
                    c.COM_CUSTOMER_ID == request.COM_CUSTOMER_ID.Value);

            if (!customerExists)
            {
                return NotFound("Customer tidak ditemukan.");
            }

            // Validasi Items
            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("Minimal harus ada 1 item.");
            }

            // Validasi setiap item
            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ITEM_NAME))
                {
                    return BadRequest("Nama item tidak boleh kosong.");
                }

                if (item.QUANTITY <= 0)
                {
                    return BadRequest("Quantity harus lebih dari 0.");
                }

                if (item.PRICE <= 0)
                {
                    return BadRequest("Price harus lebih dari 0.");
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
                return NotFound("Sales Order tidak ditemukan.");
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
    }
}