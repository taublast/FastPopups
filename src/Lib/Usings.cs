global using Microsoft.Maui;
global using Microsoft.Maui.Devices;
global using Microsoft.Maui.Graphics;
global using Microsoft.Maui.Hosting;
global using FastPopups;

#if ANDROID

global using Android.App;
global using Android.Views;

#endif

#if IOS || MACCATALYST

global using UIKit;
global using CoreGraphics;

#endif


#if WINDOWS

global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Controls.Primitives;

#endif