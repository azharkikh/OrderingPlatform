using NUnit.Framework;
using OrderService.Web.Domain;

namespace UnitTests.OrderService;

[TestFixture]
public class OrderTests
{
    [Test]
    public void Order_SetsName()
    {
        var order = new Order("Test");
        Assert.That(order.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void Order_GeneratesUniqueOrderIds()
    {
        var a = new Order("A");
        var b = new Order("B");
        Assert.That(a.OrderId, Is.Not.EqualTo(b.OrderId));
    }

    [Test]
    public void Order_OrderIdIsNotEmpty()
    {
        var order = new Order("X");
        Assert.That(order.OrderId, Is.Not.EqualTo(Guid.Empty));
    }
}
