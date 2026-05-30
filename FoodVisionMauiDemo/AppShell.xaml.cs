using FoodVisionMauiDemo.Views;

namespace FoodVisionMauiDemo
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(PredictionConfirmPage), typeof(PredictionConfirmPage));
            Routing.RegisterRoute(nameof(FoodKnowledgePage), typeof(FoodKnowledgePage));
            Routing.RegisterRoute(nameof(SaveMealPage), typeof(SaveMealPage));
            Routing.RegisterRoute(nameof(NearbyFoodPage), typeof(NearbyFoodPage));
        }
    }
}
