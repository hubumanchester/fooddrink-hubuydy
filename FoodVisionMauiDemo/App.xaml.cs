using FoodVisionMauiDemo.Services;

namespace FoodVisionMauiDemo
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            new AppSettingsService().ApplyVisualSettings();

            MainPage = new AppShell();
        }
    }
}
