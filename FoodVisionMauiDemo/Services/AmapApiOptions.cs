namespace FoodVisionMauiDemo.Services
{
    public static class AmapApiOptions
    {
        public const string WebServiceApiKey = "PUT_YOUR_AMAP_WEB_SERVICE_KEY_HERE";

        public static bool HasConfiguredApiKey =>
            !string.IsNullOrWhiteSpace(WebServiceApiKey) &&
            WebServiceApiKey != "PUT_YOUR_AMAP_WEB_SERVICE_KEY_HERE";
    }
}
