namespace FoodVisionMauiDemo.Services
{
    public class NearbySearchException : Exception
    {
        public NearbySearchException(string userMessage)
            : base(userMessage)
        {
            UserMessage = userMessage;
        }

        public NearbySearchException(string userMessage, Exception innerException)
            : base(userMessage, innerException)
        {
            UserMessage = userMessage;
        }

        public string UserMessage { get; }
    }
}
