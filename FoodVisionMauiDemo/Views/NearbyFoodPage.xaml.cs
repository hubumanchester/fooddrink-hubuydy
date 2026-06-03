using System.ComponentModel;
using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class NearbyFoodPage : ContentPage, IQueryAttributable
    {
        private NearbyFoodViewModel ViewModel => (NearbyFoodViewModel)BindingContext;

        public NearbyFoodPage()
        {
            InitializeComponent();
            BindingContext = new NearbyFoodViewModel();
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ViewModel.ApplyQueryAttributes(query);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.StartShakeListening();
            await ViewModel.LoadPlacesAsync();
        }

        protected override void OnDisappearing()
        {
            ViewModel.StopShakeListening();
            base.OnDisappearing();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NearbyFoodViewModel.ShakeFeedbackPulse))
                _ = PlayShakeFeedbackAsync();
        }

        private async Task PlayShakeFeedbackAsync()
        {
            NearbyShakeFeedbackBanner.IsVisible = true;
            NearbyShakeFeedbackBanner.Opacity = 0;
            NearbyShakeFeedbackBanner.TranslationY = -8;
            NearbyShakeFeedbackBanner.Scale = 0.98;
            NearbyShakeCounterCard.Scale = 1;

            await Task.WhenAll(
                NearbyShakeFeedbackBanner.FadeTo(1, 120, Easing.CubicOut),
                NearbyShakeFeedbackBanner.TranslateTo(0, 0, 120, Easing.CubicOut),
                NearbyShakeFeedbackBanner.ScaleTo(1, 120, Easing.CubicOut),
                NearbyShakeCounterCard.ScaleTo(1.08, 110, Easing.CubicOut));

            await Task.WhenAll(
                NearbyShakeCounterCard.ScaleTo(1, 170, Easing.CubicOut),
                NearbyHeroCard.ScaleTo(1.015, 120, Easing.CubicOut),
                NearbySearchSummaryCard.ScaleTo(1.015, 120, Easing.CubicOut));

            await Task.WhenAll(
                NearbyHeroCard.ScaleTo(1, 170, Easing.CubicOut),
                NearbySearchSummaryCard.ScaleTo(1, 170, Easing.CubicOut));
        }
    }
}
