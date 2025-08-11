namespace FastPopups;

#if !WINDOWS && !IOS && !MACCATALYST && !ANDROID && !TIZEN


public static partial class PopupExtensions
{
	static void PlatformShowPopup(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{DeviceInfo.Platform}' does not support AppoMobi.Maui.Popups.Popup.");

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{DeviceInfo.Platform}' does not support AppoMobi.Maui.Popups.Popup.");
}

#endif