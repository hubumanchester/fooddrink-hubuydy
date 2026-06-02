# Design

## Visual Direction

NutriLens KG uses a premium product UI register: Apple Health clarity, Lifesum warmth, and Material 3 tactile hierarchy. The app should look like a modern mobile health tool with concise summaries, readable data, friendly food feedback, and clear action paths.

## Color

Use a broader health palette instead of a single green theme.

- Primary: deep teal for navigation, primary actions, selected states, and important health summaries.
- Secondary accent: warm amber for attention, live API and limited-data notices.
- Positive: mint/emerald for balanced and low-risk cues.
- Warning: amber for moderate risk and limited data.
- Danger: coral red for high-risk and destructive actions.
- Neutral surfaces: off-white and mist green in light mode; charcoal green and deep graphite in dark mode.

All page backgrounds, surfaces, text, chips, errors, and buttons must support `AppThemeBinding`.

## Typography

Use the existing bundled OpenSans family for reliability across MAUI Android. Use Regular for body, Semibold for headings, buttons, section titles, metrics, and key labels. Product UI favors stable readable hierarchy over decorative display fonts.

## Layout

- Pages use 24dp horizontal padding and spacious vertical rhythm.
- Main product pages should start with a short title area, then a task-oriented summary card or control group.
- Avoid long sequences of identical cards. Mix hero summary cards, compact metric tiles, list rows, chips, and primary action blocks.
- Avoid nested cards unless the inner element is a functional control, metric tile, or input surface.

## Components

- Summary cards: deep or tinted surfaces for key status, risk, scan context, nearby focus, and KG match context.
- Standard cards: neutral elevated surfaces for diary rows, settings groups, recommendations, and explanations.
- Mini tiles: compact rounded surfaces for workflow steps and quick metrics.
- Chips: pill labels with text and semantic color tints for risk tags, allergens, and statuses.
- Inputs: rounded filled surfaces, no harsh underline-only form controls.
- Buttons: pill buttons with icon plus text for primary actions, clear disabled styling, and at least 44dp touch target.
- Empty states: centered explanation, soft mark, and next action.
- Loading states: activity indicator with status text; never leave blank content.

## Page Notes

- Dashboard: daily balance summary, recent meal, and quick actions should feel like a home screen, not a list of controls.
- Vision Scan: scan flow should feel guided and trustworthy, with a clear image surface and local inference status.
- Prediction Confirm: show image, top predictions, low confidence, and manual fallback without crowding.
- Food Knowledge: prioritize confirmed food, risk chips, allergens, alternatives, and explanation.
- Save Meal: food summary, meal metadata, notes, and voice note should feel like a focused logging task.
- Daily Log: diary entries should scan quickly with time, meal type, portion, risk tags, and edit/delete actions.
- Insights: risk level and weekly trends should read like a data dashboard with clear limitations.
- Nearby Food: live location/API state and place recommendations should be direct and trustworthy.
- Settings: controls should be grouped by user intent: live services, appearance, speech, dietary profile, data, about.
