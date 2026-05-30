namespace FoodVisionMauiDemo.Services
{
    public class FoodImageClassifierException : Exception
    {
        public FoodImageClassifierException(string message)
            : base(message)
        {
        }

        public FoodImageClassifierException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
