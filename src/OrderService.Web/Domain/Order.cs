namespace OrderService.Web.Domain;

public class Order(string name)
{
    public string Name { get; set; } = name;
    public Guid OrderId { get; set; } = Guid.NewGuid();
}
