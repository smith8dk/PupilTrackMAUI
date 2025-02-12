using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting; // For SkiaSharp integration
using CommunityToolkit.Maui;              // For CommunityToolkit
using CommunityToolkit.Maui.Camera;

namespace PupilTrack;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp() // Initialize SkiaSharp
            .UseMauiCommunityToolkitCamera()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            // Initialize the CommunityToolkit MediaElement support.
            .UseMauiCommunityToolkitMediaElement();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}