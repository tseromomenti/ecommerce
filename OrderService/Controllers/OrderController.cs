using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderService.Entities;
using OrderService.Interfaces;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(
        IValidator<OrderRequest> orderRequestValidator,
        IOrderProducer producer,
        ILogger<OrderController> logger,
        IOrderRepository orderRepository) : ControllerBase
    {
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var allOrders = await orderRepository.GetAllOrders();

            return Ok(allOrders);
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            var validationResult = await orderRequestValidator.ValidateAsync(orderRequest);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var newOrder = new OrderEntity
            {
                ProductName = orderRequest.ProductName,
                Quantity = orderRequest.Quantity,
            };

            var result = await orderRepository.AddOrderAsync(newOrder);

            await producer.SendOrderCreatedMessageAsync(result);

            logger.LogInformation("Order created: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
                result.Id, result.ProductName, result.Quantity);

            return CreatedAtAction(nameof(GetOrderById), new { result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            return Ok(order);
        }

        [HttpGet("by-product/{productName}")]
        public async Task<IActionResult> GetOrderByProduct(string productName)
        {
            var orders = await orderRepository.GetOrdersByProductNameAsync(productName);
            if (!orders.Any())
            {
                return NotFound($"No orders found for product {productName}");
            }
            return Ok(orders);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderRequest updateOrderRequest)
        {
            var validationResult = await orderRequestValidator.ValidateAsync(updateOrderRequest);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var updatedOrder = new OrderEntity
            {
                Id = id,
                ProductName = updateOrderRequest.ProductName,
                Quantity = updateOrderRequest.Quantity,
            };

            var success = await orderRepository.UpdateOrderAsync(updatedOrder);
            if (!success)
            {
                return NotFound($"Order with ID {updatedOrder.Id} not found");
            }

            await producer.SendOrderUpdatedMessageAsync(updatedOrder);

            return Ok(updatedOrder);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            var deleted = await orderRepository.DeleteOrderAsync(id);
            if (!deleted)
            {
                return NotFound($"Order with ID {id} not found");
            }

            await producer.SendOrderDeletedMessageAsync(order);

            return NoContent();
        }

        [HttpGet("GetTodayDate")]
        public IActionResult GetTodayDate()
        {
            return Ok(DateTime.Today);
        }
    }
}
