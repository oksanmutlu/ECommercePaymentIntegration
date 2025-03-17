using Ecommerce.OrderService.Services;
using ECommerce.OrderService.Data;
using ECommerce.OrderService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.OrderService.Controllers
{

    [Route("api/orders")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;

        public OrdersController(OrderDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        [HttpPost("create")]
        public async Task<ActionResult<Order>> CreateOrder(
            [FromBody] OrderRequest request,
            [FromServices] BalanceService balanceService)
        {
            if (request == null || request.ProductIds == null || !request.ProductIds.Any())
            {
                return BadRequest("Invalid request. Order must contain at least one product.");
            }

            // Sipariş oluşturma
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerName = request.CustomerName,
                ProductIds = request.ProductIds,
                TotalAmount = request.ProductIds.Count * 100 // Her ürün için 100₺ baz alıyoruz
            };

            // Ödeme rezervasyonu (Preorder)
            var preorderRequest = new PreorderRequest
            {
                OrderId = order.OrderId,
                Amount = order.TotalAmount
            };

            var isPreorderSuccessful = await balanceService.PreorderPaymentAsync(preorderRequest);

            if (!isPreorderSuccessful)
            {
                return BadRequest("Payment reservation failed.");
            }

            // Siparişi veritabanına kaydet
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }


        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteOrder(
            [FromRoute] string id,
            [FromServices] BalanceService balanceService)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { error = "Order not found", message = "The specified order does not exist." });

            if (order.Status != "blocked")
                return BadRequest(new { error = "Invalid status", message = "Order must be in 'blocked' state to complete." });

            // Balance Management Complete Payment
            var completeRequest = new CompletePaymentRequest { OrderId = order.OrderId };
            var isCompleteSuccessful = await balanceService.CompletePaymentAsync(completeRequest);

            if (!isCompleteSuccessful)
                return BadRequest(new { error = "Payment failed", message = "Could not complete payment." });

            // Siparişi tamamlandı olarak işaretle
            order.Status = "completed";
            order.CompletedAt = DateTime.UtcNow;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order completed successfully", order });
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(
            [FromRoute] string id,
            [FromServices] BalanceService balanceService)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { error = "Order not found", message = "The specified order does not exist." });

            if (order.Status != "pending" && order.Status != "blocked")
                return BadRequest(new { error = "Invalid status", message = "Order cannot be cancelled at this stage." });

            // Balance Management Preorder Cancellation
            var cancelRequest = new CancelPaymentRequest { OrderId = order.OrderId };
            var isCancelSuccessful = await balanceService.CancelPaymentAsync(cancelRequest);

            if (!isCancelSuccessful)
                return BadRequest(new { error = "Cancellation failed", message = "Could not cancel payment." });

            // Siparişi iptal edildi olarak işaretle
            order.Status = "cancelled";
            order.CancelledAt = DateTime.UtcNow;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order cancelled successfully", order });
        }
    }
}
