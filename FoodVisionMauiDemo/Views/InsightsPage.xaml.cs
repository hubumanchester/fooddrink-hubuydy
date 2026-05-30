using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class InsightsPage : ContentPage
    {
        private InsightsViewModel ViewModel => (InsightsViewModel)BindingContext;

        public InsightsPage()
        {
            InitializeComponent();
            BindingContext = new InsightsViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadAsync();
        }
    }
}
