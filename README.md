# NutriLens KG

**NutriLens KG: Vision-Based Food Scanner and Dietary Risk Assistant**

NutriLens KG is a cross-platform **Food and Drink** mobile application built with **C#**, **XAML**, **.NET MAUI**, and **MVVM**. It demonstrates a full local-first nutrition workflow:

```text
Scan -> Recognise -> Confirm -> Explain -> Save -> Analyse -> Recommend -> Find Nearby
```

The app recognises food images with a local **MobileNetV2 Food-101 ONNX** model, lets the user confirm a predicted food, maps it to a local food knowledge graph, explains likely dietary risks, stores meal logs locally, analyses recent risk patterns, recommends alternatives, and searches live nearby food places through the **AMap Web Service API**.

The solution name and root namespace are still `FoodVisionMauiDemo` because the project was evolved from a working image-recognition demo. The app title, UI, flow, and README use the final product name: **NutriLens KG**.

## Main User Flow

1. Open the **Scan** tab.
2. Take a food photo or pick an image from the gallery.
3. Run local ONNX inference.
4. Review the Top-3 Food-101 predictions.
5. Confirm one food label or use manual selection when needed.
6. Open the food knowledge explanation page.
7. Review risk tags, allergens, ingredients, alternatives, and explanation text.
8. Continue to **Save Meal**.
9. Choose meal type, portion, notes, and optionally record a voice note.
10. Save the meal to SQLite.
11. Review records in **Daily Log**.
12. Open **Insights** to view 7-day dietary balance signals.
13. Use recommendations or open **Nearby Food** to search live nearby options.

## Core Features

- Local food recognition using `mobilenet_v2_food101.onnx`.
- Food-101 label validation from `food101_labels.txt`.
- Image capture and image picking.
- Image preview before analysis.
- Top-3 prediction display with label and confidence.
- Prediction confirmation screen.
- Manual Food-101 label selection for low-confidence or incorrect predictions.
- Local food knowledge graph from `food_knowledge_graph.json`.
- Fallback explanation when a food is not covered by the local KG.
- Risk tags, allergens, common ingredients, better alternatives, and explanation text.
- Meal saving with meal type, portion, notes, image path, predictions, and KG snapshot.
- Voice note recording and playback on Android.
- Daily Log with today/recent records, image thumbnails, editable meal details, and delete confirmation.
- 7-day risk analysis for sugar, fat, salt, and refined carbohydrate load.
- Today balance signal: Low / Moderate / High.
- Alternative food recommendations based on the dominant risk signal.
- Nearby food search with real Geolocation + AMap Web Service API.
- Custom nearby search keyword support.
- Settings with Preferences persistence.
- Dark mode, font size settings, accessible labels, loading states, and user-facing error messages.

## Page Map

| Page | Purpose |
| --- | --- |
| `DashboardPage` | Home summary, quick flow entry, daily balance snapshot, recent meal context. |
| `VisionScanPage` | Camera/gallery input, image preview, local ONNX analysis, Top-3 results. |
| `PredictionConfirmPage` | Confirm one prediction, handle empty/low-confidence prediction states, manual label selection. |
| `FoodKnowledgePage` | Show KG explanation, risks, allergens, ingredients, alternatives, and TTS explanation. |
| `SaveMealPage` | Select meal type/portion, add notes, record/play voice note, save to SQLite. |
| `DailyLogPage` | Load records from SQLite, show today/recent meals, update details, delete records. |
| `InsightsPage` | Show 7-day risk scores, today balance signal, reasons, recommendations, and shake refresh. |
| `NearbyFoodPage` | Use live location + AMap API, recommended/custom keywords, refresh and shake search. |
| `SettingsPage` | Preferences, live API switch, appearance, TTS, dietary profile, data clearing, model info. |

## Navigation

The app uses MAUI Shell with a bottom `TabBar`:

- Dashboard
- Scan
- Daily Log
- Insights
- Settings

Detail pages are reached through Shell routes and are not placed in the main TabBar:

- `PredictionConfirmPage`
- `FoodKnowledgePage`
- `SaveMealPage`
- `NearbyFoodPage`

## Six Hardware Features

| Hardware feature | Where to demonstrate | Notes |
| --- | --- | --- |
| Camera | `Scan` -> `Take Photo` | Uses device camera or emulator camera support. |
| Geolocation | `Nearby Food` | Uses real location only. No demo location and no fallback nearby JSON. |
| Text-to-Speech | `FoodKnowledgePage` -> `Read Explanation`; `InsightsPage` -> `Read Risk Summary`; `SettingsPage` -> test TTS | Controlled by the TTS setting. Android emulator sound depends on installed TTS/audio support. |
| Microphone | `SaveMealPage` -> `Record Voice Note` / `Play Voice Note` | Saves WAV files in app data and supports playback. Best demonstrated on a real Android device. |
| Shake / Accelerometer | `InsightsPage` and `NearbyFoodPage` | Shaking refreshes recommendations or nearby places and updates an on-screen shake count/status. |
| Haptic Feedback / Vibration | Save Meal success and high-risk insight warning | Save success uses a stronger 500ms vibration; high-risk warning uses a two-pulse pattern. |

## Data Storage

NutriLens KG demonstrates three local storage methods.

### 1. Preferences

Implemented in:

```text
FoodVisionMauiDemo/Services/AppSettingsService.cs
FoodVisionMauiDemo/ViewModels/SettingsViewModel.cs
```

Stored settings include:

- Use Live API
- TTS enabled
- TTS speed
- Dark mode
- Font size: Normal / Large / Extra Large
- Dietary preference: None / Gluten-free / Dairy-free
- Avoid allergens: Gluten / Dairy / Nuts / Egg
- Save Scan History

### 2. File System

Uses:

```text
FileSystem.Current.AppDataDirectory
```

Implemented in:

```text
FoodVisionMauiDemo/Services/ImageStorageService.cs
FoodVisionMauiDemo/Services/VoiceNoteService.cs
FoodVisionMauiDemo/Services/VoiceNotePlaybackService.cs
```

Local file folders:

```text
AppDataDirectory/images/
AppDataDirectory/audio/
```

Food images and voice notes are stored as files. SQLite stores only the file paths and metadata, not binary blobs.

### 3. SQLite

Implemented with `sqlite-net-pcl`.

Main files:

```text
FoodVisionMauiDemo/Data/AppDatabase.cs
FoodVisionMauiDemo/Repositories/MealLogRepository.cs
```

Database path:

```text
AppDataDirectory/nutrilenskg.db3
```

Main persisted models:

- `ScanRecord`
- `PredictionResult`
- `FoodNodeSnapshot`

Saved meal records include:

- Confirmed food label and display name
- Image path
- Created time
- Meal type
- Portion
- Notes
- Voice note path and file size
- Top-3 prediction results
- KG snapshot: risk tags, risk scores, allergens, alternatives, explanation

## Local Model And Raw Assets

Required raw assets:

```text
FoodVisionMauiDemo/Resources/Raw/mobilenet_v2_food101.onnx
FoodVisionMauiDemo/Resources/Raw/food101_labels.txt
FoodVisionMauiDemo/Resources/Raw/food_knowledge_graph.json
```

The project packages raw assets through:

```xml
<MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
```

Runtime resource rules:

- The ONNX model path is never hard-coded as an absolute local path.
- The model and labels are opened through MAUI app package APIs.
- `food101_labels.txt` must contain exactly 101 labels.
- Missing model, missing labels, invalid label count, or ONNX inference failure results in a user-friendly message instead of a crash.

## Food Knowledge Graph

The local KG file is:

```text
FoodVisionMauiDemo/Resources/Raw/food_knowledge_graph.json
```

Each food node can contain:

- `foodKey`
- `displayName`
- `cuisine`
- `tags`
- `riskScores`
- `allergens`
- `ingredients`
- `alternatives`
- `explanation`

If a confirmed food label is not covered by the local KG, `KnowledgeGraphService` returns a fallback result such as:

```text
This food is not fully covered in the local knowledge graph yet.
```

This keeps the Scan -> Confirm -> Explain flow stable even for labels outside the demo KG.

## Risk Analysis System

Implemented in:

```text
FoodVisionMauiDemo/Services/RiskAnalysisService.cs
FoodVisionMauiDemo/Services/RecommendationService.cs
FoodVisionMauiDemo/Services/RiskLabelFormatter.cs
```

The current system uses four risk dimensions:

- Sugar load
- Fat load
- Salt load
- Refined carb load

Risk scoring uses:

- Per-food `riskScores` from the local KG, scaled from 0 to 10.
- Portion weights:
  - Small = 0.5
  - Medium = 1.0
  - Large = 1.5
- Recency weighting for the last seven days.
- Protective factors from balanced/high-protein foods.
- A co-occurrence multiplier when multiple strong risks appear together.

The app calculates:

- Weekly scores for the four dimensions.
- Trend labels such as increasing or stable.
- A normalised risk score from 0 to 100.
- Today balance signal: Low / Moderate / High.
- A plain-language reason explaining the result.

The Insights page explicitly frames this as a **dietary balance indicator**, not medical advice.

## Nearby Food Search

Implemented in:

```text
FoodVisionMauiDemo/Services/LocationService.cs
FoodVisionMauiDemo/Services/RiskToPlaceQueryService.cs
FoodVisionMauiDemo/Services/AmapPlaceSearchService.cs
FoodVisionMauiDemo/ViewModels/NearbyFoodViewModel.cs
```

Nearby search uses:

```text
https://restapi.amap.com/v3/place/around
```

Location format:

```text
longitude,latitude
```

Important behavior:

- Uses real Geolocation.
- Uses the live AMap Web Service API.
- Does not use demo location.
- Does not use local nearby fallback JSON.
- Supports suggested keywords based on the dominant risk tag.
- Supports custom keywords typed by the user.
- Supports refresh and shake-to-refresh.

Example risk-to-query mapping:

| Risk signal | Suggested keywords |
| --- | --- |
| high_sugar | 轻食 / 沙拉 / 健康餐 |
| high_fat | 沙拉 / 轻食 / 日料 |
| high_salt | 轻食 / 健康餐 / 沙拉 |
| high_carb | 健身餐 / 沙拉 / 蛋白餐 |
| balanced | 餐厅 / 咖啡 / 超市 |

## AMap API Key Configuration

The public source code intentionally keeps a placeholder:

```csharp
PUT_YOUR_AMAP_WEB_SERVICE_KEY_HERE
```

For local testing, create this ignored file:

```text
FoodVisionMauiDemo/Services/AmapApiOptions.local.cs
```

Use this content:

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

`.gitignore` excludes this local file:

```text
FoodVisionMauiDemo/Services/AmapApiOptions.local.cs
```

If the key is missing, wrong, expired, or disabled, the Nearby page shows a user-facing error state instead of crashing.

## Error Handling Coverage

The app provides user-friendly messages for:

- Analysing without selecting or taking an image.
- Camera unavailable.
- User cancels camera/photo picker.
- Image decode or preprocessing failure.
- Missing ONNX model.
- ONNX Runtime/native library loading failure.
- Missing labels file.
- Invalid Food-101 label count.
- ONNX inference failure.
- Empty Top-3 prediction state.
- Missing navigation parameters.
- Missing/invalid KG JSON.
- Confirmed food not covered by the KG.
- Save Meal without meal type.
- Save Meal without portion.
- SQLite save/load/update/delete failure.
- Empty Daily Log.
- Empty Insights data.
- Use Live API disabled.
- Location permission denied.
- Location unavailable or timeout.
- AMap API key missing or invalid.
- AMap `status = 0`.
- AMap returns zero results.
- Network timeout or no network.
- TTS unavailable.
- Microphone permission denied or recording unavailable.
- Shake/accelerometer unavailable.
- Haptic/vibration unsupported.

Error text is written for normal users. Technical exception details are kept in debug output when useful.

## Settings

Implemented Settings features:

- Use Live API
- Dark Mode
- Font Size: Normal / Large / Extra Large
- TTS Enabled
- TTS Speed
- Test TTS
- Dietary Preference: None / Gluten-free / Dairy-free
- Avoid Allergens: Gluten / Dairy / Nuts / Egg
- Save Scan History
- Clear Local Data
- About Model: Local MobileNetV2 Food-101 ONNX inference

Settings are stored with MAUI Preferences and are re-applied when the app starts.

## Accessibility And UI

The app uses a polished health-tool UI direction: calm, trustworthy, data-focused, and mobile-friendly.

Accessibility and UI features include:

- Dark mode support.
- Large and extra-large font settings.
- Semantic descriptions for key buttons, image areas, cards, and hardware actions.
- Risk levels shown with text and color, not color alone.
- User-facing loading states with ActivityIndicator/status text.
- Empty states with next actions.
- Touch-friendly buttons and controls.
- Card-based layouts using MAUI `Border` styles and shared resource dictionaries.

Shared UI resources:

```text
FoodVisionMauiDemo/Resources/Styles/Colors.xaml
FoodVisionMauiDemo/Resources/Styles/Styles.xaml
```

## Android Permissions

Android permissions are declared in:

```text
FoodVisionMauiDemo/Platforms/Android/AndroidManifest.xml
```

Permissions include:

- `CAMERA`
- `RECORD_AUDIO`
- `VIBRATE`
- `INTERNET`
- `ACCESS_NETWORK_STATE`
- `ACCESS_FINE_LOCATION`
- `ACCESS_COARSE_LOCATION`

## Requirements

Recommended environment:

- Visual Studio 2022/2026 with .NET MAUI workload, or .NET CLI with MAUI workload.
- .NET 8 SDK.
- Android SDK and Android emulator/device.
- Android API level compatible with .NET MAUI.
- Optional Windows target for second-device/form-factor demonstration.
- AMap Web Service API key for live nearby search.

The app is mainly tested and demonstrated on Android. The project still contains iOS and MacCatalyst target frameworks from the original MAUI template, but the coursework demo can build the Android target directly.

## How To Run In Visual Studio

1. Open:

   ```text
   FoodVisionMauiDemo.sln
   ```

2. Restore NuGet packages.
3. Confirm these raw files exist:

   ```text
   FoodVisionMauiDemo/Resources/Raw/mobilenet_v2_food101.onnx
   FoodVisionMauiDemo/Resources/Raw/food101_labels.txt
   FoodVisionMauiDemo/Resources/Raw/food_knowledge_graph.json
   ```

4. Configure the AMap key if testing Nearby Food.
5. Select an Android emulator or Android device.
6. Build and run.

If Visual Studio shows iOS or MacCatalyst target support messages, build/run the Android target only. Those messages are platform-target issues, not app-code errors for the Android demo.

## How To Build From CLI

From the repository root:

```bash
dotnet restore FoodVisionMauiDemo.sln
dotnet build FoodVisionMauiDemo/FoodVisionMauiDemo.csproj \
  -f net8.0-android \
  -p:TargetFrameworks=net8.0-android
```

On macOS, if using a local dotnet installation and Android SDK:

```bash
DOTNET_ROOT=/Users/yudongyang/.dotnet \
ANDROID_HOME=/Users/yudongyang/Library/Android/sdk \
ANDROID_SDK_ROOT=/Users/yudongyang/Library/Android/sdk \
/Users/yudongyang/.dotnet/dotnet build FoodVisionMauiDemo/FoodVisionMauiDemo.csproj \
  -f net8.0-android \
  -p:TargetFrameworks=net8.0-android
```

To deploy/run on a connected emulator/device:

```bash
DOTNET_ROOT=/Users/yudongyang/.dotnet \
ANDROID_HOME=/Users/yudongyang/Library/Android/sdk \
ANDROID_SDK_ROOT=/Users/yudongyang/Library/Android/sdk \
/Users/yudongyang/.dotnet/dotnet build FoodVisionMauiDemo/FoodVisionMauiDemo.csproj \
  -f net8.0-android \
  -t:Run \
  -p:TargetFrameworks=net8.0-android
```

## NuGet Packages

Current project dependencies include:

- `Microsoft.Maui.Controls`
- `Microsoft.Maui.Controls.Compatibility`
- `Microsoft.Extensions.Logging.Debug`
- `Microsoft.ML.OnnxRuntime`
- `Microsoft.ML.OnnxRuntime.Managed`
- `SkiaSharp`
- `sqlite-net-pcl`

The Android ONNX native runtime is packaged through the ONNX Runtime Android AAR reference in the `.csproj`.

## Code Quality

The project follows MVVM-oriented separation:

- `Views/`: XAML UI and minimal page lifecycle/event bridging.
- `ViewModels/`: state, commands, validation, and navigation coordination.
- `Services/`: ONNX classification, KG loading, image/audio storage, risk analysis, recommendations, AMap API, geolocation, TTS, shake, vibration/haptics.
- `Repositories/`: SQLite data operations.
- `Data/`: database connection and table initialization.
- `Models/`: prediction, KG, meal log, nearby place, risk, and voice-note models.

Code quality practices:

- Business logic is not concentrated in `MainPage.xaml.cs`.
- ONNX preprocessing and inference are contained in `FoodImageClassifierService`.
- SQLite access is isolated in `MealLogRepository` and `AppDatabase`.
- API key placeholder is public, real local key file is ignored.
- Async flows have exception handling and user-facing status messages.
- Raw assets are loaded from app package resources, not absolute local paths.
- Real nearby search failures are handled without fallback fake data.

### Analyzer Note

If demonstrating the coursework-required CommunityToolkit MAUI analyzer in Visual Studio, use a .NET 8 compatible Toolkit version such as:

```text
CommunityToolkit.Maui 9.1.1
```

That package contains:

```text
CommunityToolkit.Maui.Analyzers
```

In Visual Studio, confirm it under:

```text
Dependencies -> Analyzers
```

Then rebuild the Android target and check the Error List for analyzer warnings. This analyzer is one part of the code-quality evidence; the architecture separation above is also important.

## Project Structure

```text
NutriLens KG/
  FoodVisionMauiDemo.sln
  README.md
  GITHUB_SUBMISSION.md
  .gitignore
  FoodVisionMauiDemo/
    App.xaml
    AppShell.xaml
    MauiProgram.cs
    PRODUCT.md
    DESIGN.md
    Data/
      AppDatabase.cs
    Models/
      FoodPrediction.cs
      FoodKnowledgeNode.cs
      ScanRecord.cs
      PredictionResult.cs
      FoodNodeSnapshot.cs
      NearbyPlace.cs
      RiskAnalysisResult.cs
      VoiceNoteInfo.cs
      ...
    Repositories/
      MealLogRepository.cs
    Services/
      FoodImageClassifierService.cs
      KnowledgeGraphService.cs
      ImageStorageService.cs
      RiskAnalysisService.cs
      RecommendationService.cs
      AmapPlaceSearchService.cs
      LocationService.cs
      SpeechService.cs
      VoiceNoteService.cs
      VoiceNotePlaybackService.cs
      ShakeService.cs
      FeedbackService.cs
      AppSettingsService.cs
      ...
    ViewModels/
      DashboardViewModel.cs
      VisionScanViewModel.cs
      PredictionConfirmViewModel.cs
      FoodKnowledgeViewModel.cs
      SaveMealViewModel.cs
      DailyLogViewModel.cs
      InsightsViewModel.cs
      NearbyFoodViewModel.cs
      SettingsViewModel.cs
    Views/
      DashboardPage.xaml
      VisionScanPage.xaml
      PredictionConfirmPage.xaml
      FoodKnowledgePage.xaml
      SaveMealPage.xaml
      DailyLogPage.xaml
      InsightsPage.xaml
      NearbyFoodPage.xaml
      SettingsPage.xaml
    Resources/
      Raw/
      Styles/
      Images/
      Fonts/
    Platforms/
      Android/
      iOS/
      MacCatalyst/
      Windows/
```

## Manual Demo Checklist

### Main flow

- Start the app.
- Open Scan.
- Pick or take a food image.
- Run Analyse Food.
- Confirm one Top-3 prediction.
- Read the Food Knowledge page.
- Continue to Save Meal.
- Choose meal type and portion.
- Add notes and optional voice note.
- Save the meal.
- Confirm the record appears in Daily Log.
- Open Insights and review the 7-day risk summary.
- Open Nearby Food and run live search if AMap is configured.

### Error handling

- Tap Analyse without an image.
- Save without meal type.
- Save without portion.
- Turn off Use Live API and open Nearby Food.
- Test missing/invalid AMap key.
- Test location permission denial.
- Test empty Daily Log / empty Insights state.

### Hardware

- Camera: scan photo.
- Geolocation: nearby live search.
- TTS: read explanation or risk summary.
- Microphone: record and play voice note.
- Shake: refresh Insights or Nearby Food.
- Vibration/Haptic: save meal and high-risk insight warning.

### Storage

- Preferences: change Settings, restart, confirm they persist.
- File System: save image/voice note, confirm the app still references them.
- SQLite: save, edit, delete Daily Log records.

### Second device/form factor

For a second-device demonstration, run the same app on an Android tablet emulator. The source code does not need to change; the same Shell navigation and pages should run on the larger form factor.

## Known Demo Notes

- Android emulator audio input/output can be inconsistent. TTS and microphone are best demonstrated on a real Android device.
- Emulator vibration may not be physically visible; show the page action and code path if needed.
- Nearby Food requires real location permission and a valid AMap Web Service API key.
- If full solution build shows iOS/MacCatalyst target support messages, build the Android target directly for the Android demo.
- The app is a dietary balance assistant and demonstration project, not a medical diagnosis tool.

## GitHub Submission Notes

Suggested commit title:

```text
feat: complete NutriLens KG final MAUI app
```

Suggested commit body:

```text
- Implement the final NutriLens KG flow from scan to nearby recommendations.
- Preserve local MobileNetV2 Food-101 ONNX inference and Top-3 prediction display.
- Add KG explanations, SQLite meal persistence, Daily Log, 7-day insights, nearby AMap live search, custom keyword search, settings, hardware demos, and accessibility polish.
- Keep real AMap API keys out of public source code.
```

Before submission, verify the Android app in Visual Studio or with the CLI and test hardware-dependent features on a real device when possible.
