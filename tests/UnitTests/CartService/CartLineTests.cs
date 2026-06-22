using NUnit.Framework;
using CartService.Web.Domain;

namespace UnitTests.CartService;

[TestFixture]
public class CartLineTests
{
    [Test]
    public void CartLine_SetsProductName()
    {
        var line = new CartLine("Pizza");
        Assert.That(line.ProductName, Is.EqualTo("Pizza"));
    }

    [Test]
    public void CartLine_GeneratesUniqueIds()
    {
        var a = new CartLine("A");
        var b = new CartLine("B");
        Assert.That(a.OrderId, Is.Not.EqualTo(b.OrderId));
        Assert.That(a.ProductId, Is.Not.EqualTo(b.ProductId));
    }

    [Test]
    public void CartLine_IdsAreNotEmpty()
    {
        var line = new CartLine("X");
        Assert.That(line.OrderId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(line.ProductId, Is.Not.EqualTo(Guid.Empty));
    }
}
