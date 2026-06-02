# NutriLens KG

NutriLens KG is a cross-platform .NET MAUI Food and Drink app:

**Vision-Based Food Scanner and Dietary Risk Assistant**

The app runs local food image recognition with a MobileNetV2 Food-101 ONNX model, maps confirmed food labels to a small local food knowledge graph, saves meals locally, analyses recent dietary risk patterns, recommends alternatives, and can search nearby balanced food options through the AMap Web Service API.

## Main User Flow

Scan -> Recognise -> Confirm -> Explain -> Save -> Analyse -> Recommend -> Find Nearby

1. Scan or pick a food image.
2. Run local ONNX inference and show Top-3 Food-101 predictions.
3. Confirm the best food label.
4. Read local food knowledge graph information.
5. Save a meal record with meal type, portion, notes, and optional voice note.
6. Review Daily Log records from SQLite.
7. View 7-day high sugar / high fat / high salt / high carb trends.
8. Find nearby balanced options with live Geolocation + AMap POI search.

## Features

- Local MobileNetV2 Food-101 ONNX inference.
- Top-3 prediction confirmation flow with low-confidence warning and manual Food-101 label selection.
- Local `food_knowledge_graph.json` explanation with fallback when a food is not covered. The seed KG now covers about 30 demo foods.
- Nutrition risk tags, allergens, ingredients, alternatives, and explanation text.
- SQLite meal logging with prediction results and KG snapshot.
- Daily Log with today's meals, recent 7-day meals, image thumbnails, notes, voice-note status, inline editing, and delete confirmation.
- 7-day dietary balance scoring with portion weights, trend labels, and a normalised risk score:
  - Small = 0.5
  - Medium = 1.0
  - Large = 1.5
- Today risk level: Low / Moderate / High.
- Alternative recommendations based on the dominant risk tag.
- Live nearby food recommendations through AMap POI around search.
- Settings saved with MAUI Preferences.
- Dark Mode, font size scaling, and accessibility descriptions for key UI elements.

## Hardware Demonstration Points

- **Camera**: `Scan` tab, `Take Photo`.
- **Geolocation**: `Nearby Food`, live location before AMap around search.
- **Text-to-Speech**: `FoodKnowledgePage` -> `Read Explanation`; `InsightsPage` -> `Read Risk Summary`.
- **Microphone**: `SaveMealPage` -> `Record Voice Note`.
- **Shake / Accelerometer**: `InsightsPage`, shake the device to refresh recommendations.
- **Haptic Feedback / Vibration**: successful meal save; high-risk insight warning.

## Data Storage

- **Preferences**
  - Implemented in `Services/AppSettingsService.cs`.
  - Stores Use Live API, TTS enabled, TTS speed, Dark Mode, font size, dietary preference, avoid-allergen toggles, and Save Scan History.

- **File System**
  - Uses `FileSystem.Current.AppDataDirectory`.
  - Images are copied to `AppDataDirectory/images/`.
  - Voice notes are saved to `AppDataDirectory/audio/`.

- **SQLite**
  - Implemented with `sqlite-net-pcl`.
  - Database path: `AppDataDirectory/nutrilenskg.db3`.
  - Main tables: `ScanRecord`, `PredictionResult`, `FoodNodeSnapshot`.

## How To Run

Recommended: Visual Studio 2022/2026 with .NET 8 and the .NET MAUI workload.

1. Open `FoodVisionMauiDemo.sln`.
2. Restore NuGet packages.
3. Confirm required model files exist in `FoodVisionMauiDemo/Resources/Raw/`.
4. Configure the AMap Web Service API key if testing nearby search.
5. Select an Android emulator or Android device.
6. Build and run the app.

CLI, if .NET MAUI is installed:

```bash
dotnet workload restore
dotnet restore FoodVisionMauiDemo.sln
dotnet build FoodVisionMauiDemo/FoodVisionMauiDemo.csproj -f net8.0-android
```

## NuGet Packages

- `Microsoft.Maui.Controls`
- `Microsoft.Maui.Controls.Compatibility`
- `Microsoft.Extensions.Logging.Debug`
- `Microsoft.ML.OnnxRuntime`
- `SkiaSharp`
- `sqlite-net-pcl`

## Required Model And Raw Files

These files must be packaged as MAUI raw assets:

- `FoodVisionMauiDemo/Resources/Raw/mobilenet_v2_food101.onnx`
- `FoodVisionMauiDemo/Resources/Raw/food101_labels.txt`
- `FoodVisionMauiDemo/Resources/Raw/food_knowledge_graph.json`

`food101_labels.txt` must contain exactly 101 labels. The classifier validates this at runtime and shows a user-friendly error if the file is missing or invalid.

## AMap API Key Configuration

The public code intentionally keeps a placeholder key:

```csharp
PUT_YOUR_AMAP_WEB_SERVICE_KEY_HERE
```

For local testing, create this ignored file:

`FoodVisionMauiDemo/Services/AmapApiOptions.local.cs`

```csharp
namespace FoodVisionMauiDemo.Services;

public static partial class AmapApiOptions
{
    static partial void GetLocalWebServiceApiKey(ref string apiKey)
    {
        apiKey = "YOUR_REAL_AMAP_WEB_SERVICE_KEY";
    }
}
```

The file is ignored by `.gitignore`, so a real API key should not be committed. If the key is missing or invalid, `NearbyFoodPage` shows a friendly unavailable/error state instead of crashing.

## Project Structure

```text
FoodVisionMauiDemo/
  Data/                 SQLite connection and table initialization
  Models/               Prediction, KG, meal log, risk, nearby, voice note models
  Repositories/         Meal log repository
  Services/             ONNX, KG, storage, risk, recommendation, AMap, location, hardware services
  ViewModels/           MVVM page state and commands
  Views/                XAML pages
  Resources/Raw/        ONNX model, labels, local KG JSON
  Platforms/            Android/iOS/MacCatalyst permissions and platform config
```

## Error Handling Coverage

The app shows user-facing messages for:

- Analyse without an image.
- Camera unsupported, permission denied, or capture cancelled.
- Image decode/preprocess failure.
- Missing ONNX model.
- Missing or invalid Food-101 labels.
- Missing/invalid food knowledge graph JSON.
- Food not covered by the local KG.
- Missing meal type or portion.
- SQLite save/load/delete failures.
- Empty Daily Log and empty Insights data.
- Live API disabled.
- Location permission denied or location unavailable.
- Missing/invalid AMap key.
- AMap `status = 0`, no results, timeout, or network failure.
- TTS, microphone, shake, haptic, or vibration unsupported on a device.

## GitHub Submission Notes

Suggested commit title:

```text
feat: complete NutriLens KG final MAUI app flow
```

Suggested PR/commit summary:

```text
- Implement NutriLens KG Shell app flow from scan to nearby recommendations.
- Preserve local MobileNetV2 Food-101 ONNX inference and Top-3 predictions.
- Add local KG explanations, SQLite meal logging, 7-day risk insights, AMap live nearby search, hardware demos, settings, accessibility polish, and final README.
- Keep real AMap API keys out of public code.
```

Before submission, verify the app in Visual Studio on an Android emulator or device because camera, microphone, geolocation, shake, and vibration are hardware-dependent.
