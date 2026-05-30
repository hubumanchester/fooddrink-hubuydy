using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Services;
using FoodVisionMauiDemo.Views;

namespace FoodVisionMauiDemo.ViewModels
{
    public class VisionScanViewModel : BaseViewModel
    {
        private readonly FoodImageClassifierService _classifier;
        private readonly ImageStorageService _imageStorageService;
        private byte[]? _selectedImageBytes;
        private string? _selectedImagePath;
        private ImageSource? _selectedImageSource;
        private string _statusMessage = "Ready. Take or select a food image.";
        private bool _isBusy;
        private bool _isModelReady;

        public VisionScanViewModel()
            : this(new FoodImageClassifierService(), new ImageStorageService())
        {
        }

        public VisionScanViewModel(FoodImageClassifierService classifier, ImageStorageService imageStorageService)
        {
            _classifier = classifier;
            _imageStorageService = imageStorageService;

            TakePhotoCommand = new Command(async () => await TakePhotoAsync(), () => !IsBusy);
            PickImageCommand = new Command(async () => await PickImageAsync(), () => !IsBusy);
            AnalyseFoodCommand = new Command(async () => await AnalyseFoodAsync(), () => !IsBusy);
            RetakePhotoCommand = new Command(async () => await TakePhotoAsync(), () => !IsBusy);
            ConfirmResultCommand = new Command(async () => await GoToPredictionConfirmAsync(), () => !IsBusy && HasPredictions);
        }

        public ObservableCollection<FoodPrediction> Predictions { get; } = new();

        public Command TakePhotoCommand { get; }

        public Command PickImageCommand { get; }

        public Command AnalyseFoodCommand { get; }

        public Command RetakePhotoCommand { get; }

        public Command ConfirmResultCommand { get; }

        public ImageSource? SelectedImageSource
        {
            get => _selectedImageSource;
            private set => SetProperty(ref _selectedImageSource, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    RefreshCommandStates();
            }
        }

        public bool HasImage => _selectedImageBytes is { Length: > 0 };

        public bool HasNoImage => !HasImage;

        public bool HasPredictions => Predictions.Count > 0;

        public async Task InitializeAsync()
        {
            if (_isModelReady || IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading local food model...";

                await _classifier.InitializeAsync();
                _isModelReady = true;
                StatusMessage = HasImage
                    ? "Image ready. Press Analyse Food."
                    : "Ready. Take or select a food image.";
            }
            catch (FoodImageClassifierException ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "The food recognition model could not be loaded. Please check the app resources.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TakePhotoAsync()
        {
            if (IsBusy)
                return;

            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    StatusMessage = "Camera capture is not supported on this device.";
                    return;
                }

                IsBusy = true;
                StatusMessage = "Opening camera...";

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null)
                {
                    StatusMessage = "Photo capture was cancelled.";
                    return;
                }

                await LoadSelectedImageAsync(photo, "Photo captured. Press Analyse Food.");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Photo capture was cancelled.";
            }
            catch (FeatureNotSupportedException ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Camera capture is not supported on this device.";
            }
            catch (PermissionException ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Camera permission is needed to take a food photo.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not capture a photo. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PickImageAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Opening image picker...";

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a food image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null)
                {
                    StatusMessage = "Image selection was cancelled.";
                    return;
                }

                await LoadSelectedImageAsync(result, "Image selected. Press Analyse Food.");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Image selection was cancelled.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not open this image. Please try another image.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AnalyseFoodAsync()
        {
            if (IsBusy)
                return;

            if (_selectedImageBytes == null || _selectedImageBytes.Length == 0)
            {
                StatusMessage = "Please take or select a food image first.";
                return;
            }

            if (!_isModelReady)
                await InitializeAsync();

            if (!_isModelReady)
                return;

            var imageBytes = _selectedImageBytes;

            try
            {
                IsBusy = true;
                ClearPredictions();
                StatusMessage = "Analysing food image...";

                var predictions = await Task.Run(() => _classifier.PredictTopKAsync(imageBytes, 3));

                foreach (var prediction in predictions)
                    Predictions.Add(prediction);

                OnPropertyChanged(nameof(HasPredictions));
                ConfirmResultCommand.ChangeCanExecute();
                StatusMessage = Predictions.Count > 0
                    ? $"Best match: {Predictions[0].DisplayLabel}"
                    : "No food predictions were returned. Please try another image.";
            }
            catch (FoodImageClassifierException ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not analyse the image. Please try another photo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSelectedImageAsync(FileResult fileResult, string readyMessage)
        {
            using var stream = await fileResult.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var imageBytes = ms.ToArray();
            if (imageBytes.Length == 0)
            {
                StatusMessage = "This image could not be read. Please try another photo.";
                return;
            }

            _selectedImageBytes = imageBytes;
            _selectedImagePath = null;
            SelectedImageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            ClearPredictions();

            OnPropertyChanged(nameof(HasImage));
            OnPropertyChanged(nameof(HasNoImage));
            RefreshCommandStates();

            try
            {
                _selectedImagePath = await _imageStorageService.SaveImageAsync(imageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VisionScan] Non-critical image cache save failed: {ex}");
            }

            StatusMessage = readyMessage;
        }

        private void ClearPredictions()
        {
            if (Predictions.Count == 0)
                return;

            Predictions.Clear();
            OnPropertyChanged(nameof(HasPredictions));
            ConfirmResultCommand.ChangeCanExecute();
        }

        private async Task GoToPredictionConfirmAsync()
        {
            if (Predictions.Count == 0)
            {
                StatusMessage = "Please analyse a food image before confirming a result.";
                return;
            }

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["Predictions"] = Predictions.ToList()
                };

                if (!string.IsNullOrWhiteSpace(_selectedImagePath))
                    parameters["ImagePath"] = _selectedImagePath;

                await Shell.Current.GoToAsync(nameof(PredictionConfirmPage), parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not open the confirmation page. Please try again.";
            }
        }

        private void RefreshCommandStates()
        {
            TakePhotoCommand.ChangeCanExecute();
            PickImageCommand.ChangeCanExecute();
            AnalyseFoodCommand.ChangeCanExecute();
            RetakePhotoCommand.ChangeCanExecute();
            ConfirmResultCommand.ChangeCanExecute();
        }
    }
}
