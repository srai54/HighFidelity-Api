using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighFidelity.Api.DTOs;
using HighFidelity.Api.Models;
using HighFidelity.Api.BusinessLogic;

namespace HighFidelity.Api.Controllers;

/// <summary>
/// RESTful API for the HighFidelity dashboard.
/// Controllers are thin — they parse the HTTP request, delegate to
/// the business logic layer, and format the HTTP response.
/// </summary>
[Authorize]
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardBusinessLogic _service;

    public DashboardController(IDashboardBusinessLogic service) => _service = service;

    /// <summary>Returns KPI summary cards (wallet balance, referrals, etc.).</summary>
    [HttpGet("cards")]
    public async Task<ActionResult<IReadOnlyList<DashboardCard>>> GetDashboardCards()
    {
        var cards = await _service.GetDashboardCardsAsync();
        return Ok(cards);
    }

    /// <summary>Returns revenue analytics cards with chart metadata.</summary>
    [HttpGet("revenue-cards")]
    public async Task<ActionResult<IReadOnlyList<RevenueCard>>> GetRevenueCards()
    {
        var cards = await _service.GetRevenueCardsAsync();
        return Ok(cards);
    }

    /// <summary>Returns the activity timeline feed.</summary>
    [HttpGet("activities")]
    public async Task<ActionResult<IReadOnlyList<Activity>>> GetActivities()
    {
        var activities = await _service.GetActivitiesAsync();
        return Ok(activities);
    }

    /// <summary>Returns paginated orders with search support.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<Order>>> GetOrders()
    {
        var orders = await _service.GetOrdersAsync();
        return Ok(orders);
    }

    /// <summary>Creates a new order. Invoice number is auto-assigned server-side.</summary>
    [HttpPost("orders")]
    public async Task<ActionResult<Order>> AddOrder([FromBody] NewOrderRequest request)
    {
        try
        {
            var order = await _service.AddOrderAsync(
                request.Customer, request.Country, request.Price, request.Status);
            return Created($"/api/dashboard/orders/{order.Id}", order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Bulk-deletes orders by primary keys.</summary>
    [HttpDelete("orders")]
    public async Task<ActionResult> DeleteOrders([FromQuery] int[] ids)
    {
        if (ids.Length == 0)
            return BadRequest(new { error = "Provide at least one id." });

        var (success, deleted, error) = await _service.DeleteOrdersAsync(ids);
        if (!success)
            return BadRequest(new { error });
        return Ok(new { deleted });
    }

    /// <summary>Returns traffic source distribution data.</summary>
    [HttpGet("traffic")]
    public async Task<ActionResult<IReadOnlyList<TrafficSource>>> GetTrafficSources()
    {
        var sources = await _service.GetTrafficSourcesAsync();
        return Ok(sources);
    }
}
