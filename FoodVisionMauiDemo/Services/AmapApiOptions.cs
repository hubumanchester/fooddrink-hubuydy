namespace FoodVisionMauiDemo.Services
{
    public static partial class AmapApiOptions
    {
        private const string PlaceholderApiKey = "PUT_YOUR_AMAP_WEB_SERVICE_KEY_HERE";

        public static string WebServiceApiKey
        {
            get
            {
                var apiKey = PlaceholderApiKey;
                GetLocalWebServiceApiKey(ref apiKey);
                return apiKey;
            }
        }

        public static bool HasConfiguredApiKey =>
            !string.IsNullOrWhiteSpace(WebServiceApiKey) &&
            WebServiceApiKey != PlaceholderApiKey;

        static partial void GetLocalWebServiceApiKey(ref string apiKey);
    }
}
