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
            ViewModel.StartShakeListening();
        }

        protected override void OnDisappearing()
        {
            ViewModel.StopShakeListening();
            base.OnDisappearing();
        }

        private async void OnScanFirstFoodClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//VisionScanPage");
        }
    }
}
