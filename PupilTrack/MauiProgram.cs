using CommunityToolkit.Maui;              // For general CommunityToolkit initialization
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting; // For SkiaSharp integration

namespace PupilTrack;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()            // Initialize the general toolkit
            .UseMauiCommunityToolkitCamera()      // Enable native CameraView support
            .UseMauiCommunityToolkitMediaElement()// Enable MediaElement support
            .UseSkiaSharp()                       // Initialize SkiaSharp
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
