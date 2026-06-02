# GitHub Submission Notes

Suggested commit title:

```text
feat: complete NutriLens KG final MAUI app flow
```

Suggested commit body:

```text
- Build the final NutriLens KG flow: scan, recognise, confirm, explain, save, analyse, recommend, and find nearby.
- Preserve local MobileNetV2 Food-101 ONNX inference and Top-3 prediction display.
- Add local KG explanation fallback, SQLite persistence, Daily Log, 7-day insights, AMap live nearby search, and six hardware demonstration points.
- Add settings, accessibility polish, green/teal visual theme, final README, and .gitignore rules for local secrets.
- Keep real AMap API keys out of public source code.
```

Manual verification before submission:

- Restore NuGet packages in Visual Studio.
- Build and run `net8.0-android`.
- Test camera/photo picker, ONNX Top-3 predictions, KG fallback, meal saving, Daily Log, Insights, Nearby API states, TTS, microphone recording, shake refresh, vibration/haptic, and Settings persistence.
