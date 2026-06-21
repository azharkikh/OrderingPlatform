using CartService.Web.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class CartController : ControllerBase
{
    [HttpGet(Name = "GetCartLines")]
    public IEnumerable<CartLine> Get()
    {
        return [new CartLine("Margarita 30 cm"), new CartLine("Dodster gas")];
    }
}
