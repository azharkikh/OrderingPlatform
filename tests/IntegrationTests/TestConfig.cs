namespace IntegrationTests;

public static class TestConfig
{
    public static string OrderServiceBaseUrl =>
        Environment.GetEnvironmentVariable("ORDER_SERVICE_URL") ?? "http://localhost:8080";

    public static string CartServiceBaseUrl =>
        Environment.GetEnvironmentVariable("CART_SERVICE_URL") ?? "http://localhost:8081";
}
