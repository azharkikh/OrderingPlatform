namespace CartService.Web.Domain;

public class CartLine(string name)
{
    public Guid OrderId { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; } = Guid.NewGuid();
    public string? ProductName { get; set; } = name;
}
