using Orders.DTOs;
using Orders.Models;
using Orders.Services;
using Orders.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Orders.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _repo;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<OrdersController> _logger;
        
        public OrdersController(IOrderRepository repo, IHttpClientFactory httpFactory, ILogger<OrdersController> logger)
        {
            _repo = repo;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            var orders = (await _repo.GetAllAsync())
                .Where(o => o.UserId == userId)
                .ToList();

            if (!orders.Any())
                return NotFound("No orders found for this user.");

            var productClient = _httpFactory.CreateClient("ProductApi");

            var token = HttpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                productClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }

            var results = new List<OrderDto>();
            foreach (var o in orders)
            {
                var itemsDto = new List<OrderItemDto>();
                foreach (var item in o.Items)
                {
                    string? productName = null;
                    try
                    {
                        var prodResp = await productClient.GetAsync($"/api/products/{item.ProductId}");
                        if (prodResp.IsSuccessStatusCode)
                        {
                            var prod = await prodResp.Content.ReadFromJsonAsync<ProductDto>();
                            productName = prod?.Name;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch product {ProductId}", item.ProductId);
                    }

                    itemsDto.Add(new OrderItemDto
                    {
                        ProductId = item.ProductId,
                        ProductName = productName,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }

                results.Add(new OrderDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    Items = itemsDto
                });
            }

            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetOrder(int id)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order == null) return NotFound($"Order {id} not found here.");

            UserDto? user = null;
            try
            {
                var authClient = _httpFactory.CreateClient("AuthApi");

                var token = HttpContext.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(token))
                {
                    authClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
                }

                var userResp = await authClient.GetAsync($"/api/auth/users/{order.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    user = await userResp.Content.ReadFromJsonAsync<UserDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch user {UserId}", order.UserId);
            }

            var productClient = _httpFactory.CreateClient("ProductApi");

            var authToken = HttpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authToken))
            {
                productClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken.Replace("Bearer ", ""));
            }

            var itemsDto = new List<OrderItemDto>();
            foreach (var item in order.Items)
            {
                string? productName = null;
                try
                {
                    var prodResp = await productClient.GetAsync($"/api/products/{item.ProductId}");
                    if (prodResp.IsSuccessStatusCode)
                    {
                        var prod = await prodResp.Content.ReadFromJsonAsync<ProductDto>();
                        productName = prod?.Name;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to fetch product {ProductId}: {StatusCode}",
                                           item.ProductId, prodResp.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch product {ProductId}", item.ProductId);
                }

                itemsDto.Add(new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = productName,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }

            var result = new
            {
                Id = order.Id,
                UserId = order.UserId,
                User = user,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Items = itemsDto
            };

            return Ok(result);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("You must add items to this order.");

            var productClient = _httpFactory.CreateClient("ProductApi");

            var token = HttpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                productClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }

            var items = new List<OrderItem>();
            var itemsDto = new List<OrderItemDto>();

            foreach (var i in dto.Items)
            {
                string? productName = null;

                try
                {
                    var prodResp = await productClient.GetAsync($"/api/products/{i.ProductId}");
                    if (prodResp.IsSuccessStatusCode)
                    {
                        var prod = await prodResp.Content.ReadFromJsonAsync<ProductDto>();
                        productName = prod?.Name;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to fetch product {ProductId}: {StatusCode}",
                                           i.ProductId, prodResp.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch product {ProductId}", i.ProductId);
                }

                items.Add(new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                });

                itemsDto.Add(new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = productName,
                    Quantity = i.Quantity,
                    Price = i.Price
                });
            }

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = items.Sum(i => i.Price * i.Quantity),
                Items = items
            };

            var created = await _repo.CreateAsync(order);

            var dtoResult = new OrderDto
            {
                Id = created.Id,
                UserId = created.UserId,
                OrderDate = created.OrderDate,
                TotalAmount = created.TotalAmount,
                Status = created.Status,
                Items = itemsDto
            };

            foreach(var item in created.Items)
            {
                var message = new
                {
                    OrderId = created.Id,
                    ProductId = item.ProductId,
                    QuantityOrdered = item.Quantity
                };
                var producer = HttpContext.RequestServices.GetRequiredService<ProducerService>();
                await producer.SendOrderCreatedMessageAsync(message);
            }

            return CreatedAtAction(nameof(GetOrder), new { id = created.Id }, dtoResult);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string newStatus)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order == null) return NotFound();

            order.Status = newStatus;
            await _repo.UpdateAsync(order);

            return NoContent();
        }

        [Authorize(Roles = "User")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order == null) return NotFound();

            await _repo.DeleteAsync(order);
            return NoContent();
        }
    }
}