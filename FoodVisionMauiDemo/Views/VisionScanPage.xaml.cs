using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class VisionScanPage : ContentPage
    {
        private VisionScanViewModel ViewModel => (VisionScanViewModel)BindingContext;

        public VisionScanPage()
        {
            InitializeComponent();
            BindingContext = new VisionScanViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.InitializeAsync();
        }
    }
}
