# AppoMobi.Maui.Popups Sample Application

This sample application demonstrates the usage of the **AppoMobi.Maui.Popups** library, which provides enhanced popup functionality for .NET MAUI applications.

## Features Demonstrated

### 1. Simple Popup (XAML-defined)
- Basic popup with XAML markup
- Custom styling with borders and rounded corners
- Close button functionality
- Overlay background dimming

### 2. Anchored Popup
- Popup positioned relative to a specific UI element (button)
- Demonstrates anchor-based positioning
- Custom overlay color with transparency

### 3. Centered Popup (Code-defined)
- Popup created entirely in C# code
- Centered positioning on screen
- Custom size and green theme
- Demonstrates programmatic popup creation

### 4. Custom Positioned Popup
- Top-left positioning
- Purple theme with custom colors
- Larger size (400x300)
- Shows flexibility in popup positioning

## Key Features of AppoMobi.Maui.Popups

- **Enhanced Platform Handlers**: Custom platform-specific implementations for Android, iOS, and Windows
- **Flexible Positioning**: Support for anchored, centered, and custom positioning
- **Overlay Customization**: Configurable overlay colors and transparency
- **Dismissal Options**: Tap-outside-to-dismiss functionality
- **Size Control**: Fixed sizes or content-based sizing
- **Theme Support**: Full styling and theming capabilities

## Usage

1. **Install the library**: Add project reference to `AppoMobi.Maui.Popups`
2. **Configure handlers**: Call `ConfigurePopups()` in `MauiProgram.cs`
3. **Show popups**: Use `ShowPopup()` or `ShowPopupAsync()` extension methods
4. **Create custom popups**: Inherit from `Popup` class or use XAML

## Code Examples

### Basic Usage
```csharp
var popup = new Popup
{
    Size = new Size(300, 200),
    OverlayColor = Colors.Black.WithAlpha(0.5f),
    Content = new Label { Text = "Hello from popup!" }
};

await this.ShowPopupAsync(popup);
```

### XAML Popup
```xml
<popup:Popup x:Class="MyApp.MyPopup"
             Size="300,200"
             OverlayColor="Black"
             CloseWhenBackgroundIsClicked="True">
    <!-- Your content here -->
</popup:Popup>
```

## Platform Support

- ✅ Android
- ✅ iOS  
- ✅ macOS Catalyst
- ✅ Windows

## Running the Sample

1. Ensure you have .NET 9 and MAUI workload installed
2. Open the solution in Visual Studio 2022 or Visual Studio Code
3. Build and run the `SampleApp` project
4. Test the different popup types using the buttons in the main interface

The sample demonstrates the power and flexibility of the AppoMobi.Maui.Popups library with its enhanced platform handlers that provide superior performance and reliability compared to the original Community Toolkit implementation.