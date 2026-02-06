using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(
    IPaymentGatewayService paymentGatewayService,
    IPaymentStore paymentStore) : ControllerBase
{
    [HttpPost("checkout-session")]
    [Authorize]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than zero." });
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        request.UserId = userId;
        var response = await paymentGatewayService.CreateCheckoutSessionAsync(request);
        return Ok(response);
    }

    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await paymentGatewayService.HandleStripeWebhookAsync(payload, signature);
        return Ok();
    }

    [HttpGet("{paymentId}")]
    [Authorize]
    public ActionResult<PaymentDto> GetPayment(string paymentId)
    {
        var payment = paymentStore.Get(paymentId);
        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}
