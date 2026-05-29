using FoodVisionMauiDemo.Services;
using System.Diagnostics;

namespace FoodVisionMauiDemo.Views
{
    public partial class VisionScanPage : ContentPage
    {
        private readonly FoodImageClassifierService _classifier;
        private byte[]? _currentImageBytes;

        public VisionScanPage()
        {
            InitializeComponent();
            _classifier = new FoodImageClassifierService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                StatusLabel.Text = "Loading model...";
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                await _classifier.InitializeAsync();

                StatusLabel.Text = "Ready. Take or select a food image.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusLabel.Text = "The model file could not be loaded. Please check that mobilenet_v2_food101.onnx is in Resources/Raw.";
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnTakePhotoClicked(object sender, EventArgs e)
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    StatusLabel.Text = "Camera capture is not supported on this device.";
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo == null)
                {
                    StatusLabel.Text = "Photo capture was cancelled.";
                    return;
                }

                _currentImageBytes = await ReadStreamToByteArrayAsync(photo);
                ImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_currentImageBytes));
                ClearResults();
                StatusLabel.Text = "Photo captured. Press Analyse Food.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusLabel.Text = "Could not capture photo. Please try again.";
            }
        }

        private async void OnPickTestImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a food image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null)
                {
                    StatusLabel.Text = "Image selection was cancelled.";
                    return;
                }

                _currentImageBytes = await ReadStreamToByteArrayAsync(result);
                ImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_currentImageBytes));
                ClearResults();
                StatusLabel.Text = "Image selected. Press Analyse Food.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusLabel.Text = "Could not open image. Please try another image.";
            }
        }

        private async void OnAnalyseFoodClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentImageBytes == null || _currentImageBytes.Length == 0)
                {
                    StatusLabel.Text = "Please take or select a food image first.";
                    return;
                }

                SetButtonsEnabled(false);
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                StatusLabel.Text = "Analysing...";

                var predictions = await Task.Run(() => _classifier.PredictTopKAsync(_currentImageBytes));

                if (predictions.Count > 0)
                {
                    StatusLabel.Text = $"Best match: {predictions[0].DisplayLabel}";
                    Result1.Text = $"1. {predictions[0].DisplayText}";
                    Result1.TextColor = Colors.Black;

                    if (predictions.Count > 1)
                        Result2.Text = $"2. {predictions[1].DisplayText}";
                    else
                        Result2.Text = string.Empty;

                    if (predictions.Count > 2)
                        Result3.Text = $"3. {predictions[2].DisplayText}";
                    else
                        Result3.Text = string.Empty;

                    ResultsFrame.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusLabel.Text = "Could not analyse the image. Please check the model and preprocessing pipeline.";
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                SetButtonsEnabled(true);
            }
        }

        private void ClearResults()
        {
            Result1.Text = string.Empty;
            Result2.Text = string.Empty;
            Result3.Text = string.Empty;
            ResultsFrame.IsVisible = false;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            BtnTakePhoto.IsEnabled = enabled;
            BtnPickTestImage.IsEnabled = enabled;
            BtnAnalyse.IsEnabled = enabled;
        }

        private static async Task<byte[]> ReadStreamToByteArrayAsync(FileResult fileResult)
        {
            using var stream = await fileResult.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
