# Popups for .NET MAUI

---

## About

This small private library is built on top of CommunityToolkit popups version 1, code no longer used by the toolkit itself.  It was the fastest for opening popups among different libraries, was definitely worth adding HotReload support, IgnoreSafeArea and a separate dimmer layer for all platforms.

---

## To Do

### Required

* Test orientation changes when already open
* Android anchor is acting lower by the amount for either bottom navbar or shell navbar, need to investigate
* Test and possibly fix Tizen

### Roadmap

* Add navigation stack
* Add Blur effect below 
* Add background layer custom content
* Add animations like we had in RG popups, working separately separate for overlay and content

---

## Popup Layers
When rendered your popup is using 2 layers:

* Background dimmer layer, can be totally transparent or dim your background under popup
* Popup content

### Background Layer

At all times you have a background layer filling the entire screen, layout properties will not affect it. You can change it's color by setting BackgroundColor of the popup, for example:

```xml
BackgroundColor="#66000000"
```

would create a semi-transparent dimmer.

You can hide the layer by setting popup `BackgroundColor` property to a totally transparent color.

### Popup Content

This is your logical popup, layout properties like `HorizontalOptions`, `VerticalOptions` will affect its positioning.

`IgnoreSafeArea` controls if you want to consider full screen or not to go over safe insets, like status bar etc.

## Gestures

When `CloseWhenBackgroundIsClicked` is set to `true` then tapping somewhere where gestures are not user-handled will close your popup.

## Anchor Positioning

You can position popups relative to specific UI elements. This is particularly useful for context menus, tooltips, or dropdowns that should appear next to buttons or other controls.

When you set the `Anchor` property of a popup to reference another view, the popup will be positioned relative to that anchor element instead of using the standard alignment properties (`HorizontalOptions`, `VerticalOptions`).

First, give your anchor element a name:

```xml
<Button x:Name="AnchoredButton" 
        Text="Show Anchored Popup" 
        Clicked="OnAnchoredPopupClicked" />
```

Then set the anchor when showing the popup:

```csharp
private async void OnAnchoredPopupClicked(object sender, EventArgs e)
{
    var popup = new AnchoredPopup();
    popup.Anchor = AnchoredButton; // Reference the button
    await this.ShowPopupAsync(popup);
}
```
 
---

