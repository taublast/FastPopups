# MAUI Popups

---

## TODO

### Required

* Test orientation changes when open
* Android anchor is lower by the amount for either bottom navbar or shell navbar..
* Test (and then fix) Tizen

### Optional

* Add Blur effect below on every platform
* Add animations like we had in RG popups, working separately separate for overlay and content

## Features

* Implement any popup design without restrictions
* Customize separate background layer
* Optional anchors for positioning
* Small popup opening time
* Consistent across platforms
* Supports HotReload

## Popup Layers
When rendered your popup is bringing up 2 layers:

* Background dimmer layer, can be totally transparent or dim your background under popup
* Popup content


### Background Layer

At al times you have a background layer filling the entire screen, layout properties will not affect it. You can change it's color by setting BackgroundColor of the popup, for example:

```xml
BackgroundColor="#66000000"
```

would create a semi-transparent dimmer.

You can hide the layer by setting popup `BackgroundColor` property to a totally transparent color.

### Popup Content

Layout properties like `HorizontalOptions`, `VerticalOptions` will affect your popup content.

## Anchor Positioning

The library supports anchor-based positioning, allowing you to position popups relative to specific UI elements. This is particularly useful for context menus, tooltips, or dropdowns that should appear next to buttons or other controls.

### How Anchors Work

When you set the `Anchor` property of a popup to reference another view, the popup will be positioned relative to that anchor element instead of using the standard alignment properties (`HorizontalOptions`, `VerticalOptions`).

### Setting Up Anchors

#### 1. In XAML with Code-Behind

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

#### 2. Programmatically

```csharp
var popup = new MyPopup
{
    Anchor = myButton,
    Size = new Size(250, 150),
    BackgroundColor = Color.FromArgb("#80000000")
};
await this.ShowPopupAsync(popup);
```

### Anchor Positioning Behavior

The anchor positioning system works as follows:

1. **Default Position**: By default, anchored popups appear **below** the anchor element, horizontally centered
2. **Smart Positioning**: If there's insufficient space below the anchor, the popup automatically appears **above** it
3. **Screen Boundaries**: The popup is automatically adjusted to stay within screen boundaries
4. **Cross-Platform Consistency**: Anchor positioning works identically across Android, iOS, macOS, and Windows

### Fullscreen Overlay Architecture

The library uses a fullscreen overlay architecture that ensures:

- **Proper Layering**: Anchored popups can appear over existing popups and dim the entire background
- **Accurate Coordinates**: Anchor positioning uses precise screen coordinate calculations
- **Platform Independence**: The same positioning logic works across all platforms

### Technical Implementation

The anchor positioning system:

1. **Coordinate Calculation**: Uses platform-specific APIs to get precise anchor element coordinates
   - Android: `GetLocationOnScreen()` with fullscreen dialog coordinate system
   - Windows: `TransformToVisual()` for coordinate transformation
   - iOS/macOS: Standard view hierarchy coordinate conversion

2. **Layout Calculator**: Uses `PopupLayoutCalculator.CalculateAnchoredPosition()` for consistent positioning logic across platforms

3. **Fullscreen Compatibility**: Designed to work with the fullscreen overlay system, ensuring anchors work properly even when multiple popups are stacked

### Example Usage

Here's a complete example of an anchored popup:

```xml
<!-- AnchoredPopup.xaml -->
<popups:Popup xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:popups="clr-namespace:AppoMobi.Maui.Popups;assembly=AppoMobi.Maui.Popups"
             Size="250,150"
             BackgroundColor="#80000000"
             CloseWhenBackgroundIsClicked="False">
    
    <Border BackgroundColor="LightYellow" 
            StrokeThickness="1" 
            Stroke="Orange"
            StrokeShape="RoundRectangle 8">
        <StackLayout Padding="15" Spacing="10">
            <Label Text="Anchored Popup" 
                   FontSize="16" 
                   FontAttributes="Bold" 
                   HorizontalOptions="Center" />
            <Label Text="This popup is anchored to a button!" 
                   HorizontalOptions="Center" />
            <Button Text="Got it!" 
                    BackgroundColor="Orange" 
                    TextColor="White"
                    Clicked="OnGotItClicked" />
        </StackLayout>
    </Border>
</popups:Popup>
```

### Notes

- When using anchors, `HorizontalOptions` and `VerticalOptions` are ignored for positioning
- The anchor element must be visible and properly laid out before showing the popup
- Anchor positioning works with all popup sizes and background configurations
- The fullscreen overlay ensures proper visual layering even with anchored popups

## The Code

This library was built on top of CommunityToolkit Popups v1, no longer used in the toolkit, 
found to be the fastest when comparing popup opening time between different libraries.
Consistency across platforms, HotReload support, a separate background layer were added and this library went on a separate jurney.

---

