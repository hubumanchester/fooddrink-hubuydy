using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using System.Diagnostics;

namespace FoodVisionMauiDemo.Services
{
    public class FoodImageClassifierService : IDisposable
    {
        private InferenceSession? _session;
        private string[]? _labels;
        private bool _initialized;
        private string _inputName = "input";

        private static readonly float[] Mean = [0.485f, 0.456f, 0.406f];
        private static readonly float[] Std = [0.229f, 0.224f, 0.225f];

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            // Load ONNX model from app package
            using var modelStream = await FileSystem.Current.OpenAppPackageFileAsync("mobilenet_v2_food101.onnx");
            using var ms = new MemoryStream();
            await modelStream.CopyToAsync(ms);
            var modelBytes = ms.ToArray();

            _session = new InferenceSession(modelBytes);

            // Log model metadata
            foreach (var entry in _session.InputMetadata)
            {
                _inputName = entry.Key;
                Debug.WriteLine($"[FoodImageClassifier] Input name: {entry.Key}, dimensions: [{string.Join(", ", entry.Value.Dimensions)}], type: {entry.Value.ElementType}");
            }
            foreach (var entry in _session.OutputMetadata)
            {
                Debug.WriteLine($"[FoodImageClassifier] Output name: {entry.Key}, dimensions: [{string.Join(", ", entry.Value.Dimensions)}], type: {entry.Value.ElementType}");
            }

            Debug.WriteLine("[FoodImageClassifier] Model loaded successfully.");

            // Load labels
            using var labelStream = await FileSystem.Current.OpenAppPackageFileAsync("food101_labels.txt");
            using var reader = new StreamReader(labelStream);
            var text = await reader.ReadToEndAsync();
            _labels = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                          .Select(l => l.Trim())
                          .Where(l => !string.IsNullOrEmpty(l))
                          .ToArray();

            Debug.WriteLine($"[FoodImageClassifier] Number of labels loaded: {_labels.Length}");

            if (_labels.Length != 101)
                throw new InvalidOperationException($"The labels file must contain exactly 101 labels. Found {_labels.Length}.");

            _initialized = true;

            // Optional warm-up
            try
            {
                var warmupTensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
                var warmupInput = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(_inputName, warmupTensor)
                };
                using var warmupResults = _session.Run(warmupInput);
                Debug.WriteLine("[FoodImageClassifier] Warm-up inference completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FoodImageClassifier] Warm-up failed (non-critical): {ex.Message}");
            }
        }

        public async Task<IReadOnlyList<FoodPrediction>> PredictTopKAsync(byte[] imageBytes, int topK = 3)
        {
            if (!_initialized)
                await InitializeAsync();

            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes cannot be null or empty.");

            topK = Math.Clamp(topK, 1, 101);

            Debug.WriteLine($"[FoodImageClassifier] Image byte length: {imageBytes.Length}");

            var tensor = PreprocessImage(imageBytes);

            Debug.WriteLine("[FoodImageClassifier] Preprocessing completed.");

            var input = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, tensor)
            };

            using var results = _session!.Run(input);
            var outputTensor = results.First().AsTensor<float>();
            var logits = outputTensor.ToArray();

            Debug.WriteLine("[FoodImageClassifier] Inference completed.");

            var probabilities = Softmax(logits);

            // Debug: log first 5 logits and their softmax results
            Debug.WriteLine($"[FoodImageClassifier] Logits[0..4]: {logits[0]:F4}, {logits[1]:F4}, {logits[2]:F4}, {logits[3]:F4}, {logits[4]:F4}");
            Debug.WriteLine($"[FoodImageClassifier] Logits min={logits.Min():F4}, max={logits.Max():F4}");
            Debug.WriteLine($"[FoodImageClassifier] Probabilities[0..4]: {probabilities[0]:F4}, {probabilities[1]:F4}, {probabilities[2]:F4}, {probabilities[3]:F4}, {probabilities[4]:F4}");

            var topKPredictions = GetTopK(probabilities, topK);

            Debug.WriteLine("[FoodImageClassifier] Top-3 raw results:");
            for (int i = 0; i < topKPredictions.Count; i++)
            {
                Debug.WriteLine($"  {i + 1}. {topKPredictions[i].DisplayText}");
            }

            return topKPredictions;
        }

        private DenseTensor<float> PreprocessImage(byte[] imageBytes)
        {
            using var original = SKBitmap.Decode(imageBytes);
            if (original == null)
                throw new InvalidOperationException("Could not decode image. Please try another photo.");

            Debug.WriteLine($"[Preprocess] Original: {original.Width}x{original.Height}, ColorType={original.ColorType}");

            // Resize: shorter side to 256, preserving aspect ratio
            float scale = 256.0f / Math.Min(original.Width, original.Height);
            int newW = (int)(original.Width * scale);
            int newH = (int)(original.Height * scale);

            using var resized = original.Resize(new SKImageInfo(newW, newH), SKFilterQuality.Medium);
            if (resized == null)
                throw new InvalidOperationException("Could not resize image.");

            Debug.WriteLine($"[Preprocess] Resized: {resized.Width}x{resized.Height}, ColorType={resized.ColorType}");

            // Center crop — read pixels directly from resized.Pixels SKColor[] array
            // to avoid ExtractSubset color-type compatibility issues
            int cropX = (newW - 224) / 2;
            int cropY = (newH - 224) / 2;
            SKColor[] resizedPixels = resized.Pixels;

            // Debug: sample a few pixels
            int midIdx = (cropY + 112) * newW + (cropX + 112);
            var midPixel = resizedPixels[midIdx];
            Debug.WriteLine($"[Preprocess] Center pixel at ({cropX + 112},{cropY + 112}): R={midPixel.Red} G={midPixel.Green} B={midPixel.Blue}");

            // Build NCHW tensor [1, 3, 224, 224]
            var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });

            float rMin = float.MaxValue, rMax = float.MinValue;
            float gMin = float.MaxValue, gMax = float.MinValue;
            float bMin = float.MaxValue, bMax = float.MinValue;

            for (int y = 0; y < 224; y++)
            {
                for (int x = 0; x < 224; x++)
                {
                    int srcIdx = (cropY + y) * newW + (cropX + x);
                    SKColor pixel = resizedPixels[srcIdx];

                    float r = pixel.Red / 255.0f;
                    float g = pixel.Green / 255.0f;
                    float b = pixel.Blue / 255.0f;

                    // Normalize with ImageNet mean/std
                    float rNorm = (r - Mean[0]) / Std[0];
                    float gNorm = (g - Mean[1]) / Std[1];
                    float bNorm = (b - Mean[2]) / Std[2];

                    tensor[0, 0, y, x] = rNorm;
                    tensor[0, 1, y, x] = gNorm;
                    tensor[0, 2, y, x] = bNorm;

                    if (rNorm < rMin) rMin = rNorm;
                    if (rNorm > rMax) rMax = rNorm;
                    if (gNorm < gMin) gMin = gNorm;
                    if (gNorm > gMax) gMax = gNorm;
                    if (bNorm < bMin) bMin = bNorm;
                    if (bNorm > bMax) bMax = bNorm;
                }
            }

            Debug.WriteLine($"[Preprocess] Tensor stats — R: [{rMin:F3}, {rMax:F3}]  G: [{gMin:F3}, {gMax:F3}]  B: [{bMin:F3}, {bMax:F3}]");
            Debug.WriteLine($"[Preprocess] Tensor[0,0,0,0] (R at 0,0): {tensor[0, 0, 0, 0]:F4}");
            Debug.WriteLine($"[Preprocess] Tensor[0,0,112,112] (R at center): {tensor[0, 0, 112, 112]:F4}");

            return tensor;
        }

        private static float[] Softmax(float[] logits)
        {
            float max = logits.Max();
            float[] exp = new float[logits.Length];
            float sum = 0f;

            for (int i = 0; i < logits.Length; i++)
            {
                exp[i] = MathF.Exp(logits[i] - max);
                sum += exp[i];
            }

            for (int i = 0; i < exp.Length; i++)
            {
                exp[i] /= sum;
            }

            return exp;
        }

        private IReadOnlyList<FoodPrediction> GetTopK(float[] probabilities, int topK)
        {
            return probabilities
                .Select((prob, idx) => (Index: idx, Probability: prob))
                .OrderByDescending(x => x.Probability)
                .Take(topK)
                .Select(x => new FoodPrediction
                {
                    Label = _labels![x.Index],
                    Confidence = x.Probability
                })
                .ToList();
        }

        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
            _labels = null;
            _initialized = false;
        }
    }
}
