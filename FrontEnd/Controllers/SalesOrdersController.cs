using FrontEnd.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FrontEnd.Controllers
{
    public class SalesOrdersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SalesOrdersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(
            string? keyword,
            DateTime? orderDate)
        {
            var client = _httpClientFactory.CreateClient("SalesOrderService");

            var url = "/api/orders";

            var parameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                parameters.Add($"keyword={Uri.EscapeDataString(keyword)}");
            }

            if (orderDate.HasValue)
            {
                parameters.Add($"orderDate={orderDate.Value:yyyy-MM-dd}");
            }

            if (parameters.Count > 0)
            {
                url += "?" + string.Join("&", parameters);
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Gagal mengambil data Sales Order.";
                return View(new List<SalesOrderResponse>());
            }

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var salesOrders =
                JsonSerializer.Deserialize<List<SalesOrderResponse>>(
                    json,
                    options
                ) ?? new List<SalesOrderResponse>();

            ViewBag.Keyword = keyword;
            ViewBag.OrderDate = orderDate?.ToString("yyyy-MM-dd");

            return View(salesOrders);
        }
        // GET: /SalesOrders/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = _httpClientFactory.CreateClient("CustomerService");

            var response = await client.GetAsync("/api/Customers");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Gagal mengambil data customer.";
                return View(new CreateSalesOrderRequest());
            }

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var customers =
                JsonSerializer.Deserialize<List<CustomerResponse>>(
                    json,
                    options
                ) ?? new List<CustomerResponse>();

            ViewBag.Customers = customers;

            return View(new CreateSalesOrderRequest
            {
                ORDER_DATE = DateTime.Today
            });
        }
        // POST: /SalesOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSalesOrderRequest request)
        {
            var customerClient =
                _httpClientFactory.CreateClient("CustomerService");

            var customerResponse =
                await customerClient.GetAsync("/api/Customers");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var customerJson =
                await customerResponse.Content.ReadAsStringAsync();

            var customers =
                JsonSerializer.Deserialize<List<CustomerResponse>>(
                    customerJson,
                    options
                ) ?? new List<CustomerResponse>();

            ViewBag.Customers = customers;

            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var salesOrderClient =
                _httpClientFactory.CreateClient("SalesOrderService");

            var json = JsonSerializer.Serialize(request);

            var content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response =
                await salesOrderClient.PostAsync("/api/orders", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    await response.Content.ReadAsStringAsync();

                ViewBag.Error = errorMessage;

                return View(request);
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: /SalesOrders/Edit/2
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customerClient =
                _httpClientFactory.CreateClient("CustomerService");

            var customerResponse =
                await customerClient.GetAsync("/api/Customers");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            if (!customerResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "Gagal mengambil data customer.";
                return RedirectToAction(nameof(Index));
            }

            var customerJson =
                await customerResponse.Content.ReadAsStringAsync();

            var customers =
                JsonSerializer.Deserialize<List<CustomerResponse>>(
                    customerJson,
                    options
                ) ?? new List<CustomerResponse>();

            ViewBag.Customers = customers;


            // Ambil detail Sales Order
            var salesOrderClient =
                _httpClientFactory.CreateClient("SalesOrderService");

            var response =
                await salesOrderClient.GetAsync($"/api/orders/{id}");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Sales Order tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var json =
                await response.Content.ReadAsStringAsync();

            var salesOrder =
                JsonSerializer.Deserialize<SalesOrderResponse>(
                    json,
                    options
                );

            if (salesOrder == null)
            {
                ViewBag.Error = "Data Sales Order tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var request = new CreateSalesOrderRequest
            {
                SO_NO = salesOrder.SO_NO,
                ORDER_DATE = salesOrder.ORDER_DATE,
                COM_CUSTOMER_ID = salesOrder.COM_CUSTOMER_ID,
                ADDRESS = salesOrder.ADDRESS,

                Items = salesOrder.Items
                    .Select(x => new CreateSalesOrderItemRequest
                    {
                        ITEM_NAME = x.ITEM_NAME,
                        QUANTITY = x.QUANTITY,
                        PRICE = x.PRICE
                    })
                    .ToList()
            };

            ViewBag.SalesOrderId = id;

            return View(request);
        }
        // POST: /SalesOrders/Edit/2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CreateSalesOrderRequest request)
        {
            // Ambil daftar customer untuk dropdown
            var customerClient =
                _httpClientFactory.CreateClient("CustomerService");

            var customerResponse =
                await customerClient.GetAsync("/api/Customers");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            if (customerResponse.IsSuccessStatusCode)
            {
                var customerJson =
                    await customerResponse.Content.ReadAsStringAsync();

                var customers =
                    JsonSerializer.Deserialize<List<CustomerResponse>>(
                        customerJson,
                        options
                    ) ?? new List<CustomerResponse>();

                ViewBag.Customers = customers;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SalesOrderId = id;
                return View(request);
            }

            // Kirim update ke SalesOrderService
            var salesOrderClient =
                _httpClientFactory.CreateClient("SalesOrderService");

            var json =
                JsonSerializer.Serialize(request);

            var content =
                new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

            var response =
                await salesOrderClient.PutAsync(
                    $"/api/orders/{id}",
                    content
                );

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    await response.Content.ReadAsStringAsync();

                ViewBag.Error = errorMessage;
                ViewBag.SalesOrderId = id;
                

                return View(request);
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: /SalesOrders/Delete/2
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var salesOrderClient =
                _httpClientFactory.CreateClient("SalesOrderService");

            var response =
                await salesOrderClient.DeleteAsync($"/api/orders/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    string.IsNullOrWhiteSpace(errorMessage)
                        ? "Gagal menghapus Sales Order."
                        : errorMessage;

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Sales Order berhasil dihapus.";

            return RedirectToAction(nameof(Index));
        }
        // GET: SalesOrders/Export
        [HttpGet]
        public async Task<IActionResult> Export(
            string? keyword,
            DateTime? orderDate)
        {
            var client = _httpClientFactory.CreateClient("SalesOrderService");

            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query.Add(
                    $"keyword={Uri.EscapeDataString(keyword)}"
                );
            }

            if (orderDate.HasValue)
            {
                query.Add(
                    $"orderDate={orderDate.Value:yyyy-MM-dd}"
                );
            }

            var url = "/api/orders/export";

            if (query.Count > 0)
            {
                url += "?" + string.Join("&", query);
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    "Gagal melakukan export: " + error;

                return RedirectToAction(nameof(Index));
            }

            var fileBytes =
                await response.Content.ReadAsByteArrayAsync();

            var contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            var fileName =
                $"SalesOrder_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                fileBytes,
                contentType,
                fileName
            );
        }
        [HttpPost]
        public async Task<IActionResult> CalculateItem(
    [FromBody] CreateSalesOrderItemRequest item)
        {
            var client = _httpClientFactory.CreateClient("SalesOrderService");

            var json = JsonSerializer.Serialize(item);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "/api/orders/calculate-item",
                content
            );

            var result = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = result,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        [HttpPost]
        public async Task<IActionResult> Calculate(
    [FromBody] CreateSalesOrderRequest request)
        {
            var client =
                _httpClientFactory.CreateClient("SalesOrderService");

            var json =
                JsonSerializer.Serialize(request);

            var content =
                new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

            var response =
                await client.PostAsync(
                    "/api/orders/calculate",
                    content
                );

            var result =
                await response.Content.ReadAsStringAsync();

            return Content(
                result,
                "application/json"
            );
        }
    }
}