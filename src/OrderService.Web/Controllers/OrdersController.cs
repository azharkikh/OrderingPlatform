using Microsoft.AspNetCore.Mvc;
using OrderService.Web.Domain;

namespace OrderService.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet(Name = "GetOrders")]
    public IEnumerable<Order> Get()
    {
        return [new Order("Andrew"), new Order("Alexander")];
    }
}
