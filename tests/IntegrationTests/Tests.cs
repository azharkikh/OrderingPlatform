using System.Net;
using NUnit.Framework;

namespace IntegrationTests;

[TestFixture]
public class OrderServiceTests
{
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp() =>
        _client = new HttpClient { BaseAddress = new Uri(TestConfig.OrderServiceBaseUrl) };

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task GetOrders_ReturnsOk() =>
        Assert.That((await _client.GetAsync("/Orders")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
}

[TestFixture]
public class CartServiceTests
{
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp() =>
        _client = new HttpClient { BaseAddress = new Uri(TestConfig.CartServiceBaseUrl) };

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task GetCart_ReturnsOk() =>
        Assert.That((await _client.GetAsync("/Cart")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
}
