using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting; // Add this for SkiaSharp integration
using CommunityToolkit.Maui;

namespace PupilTrack;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseSkiaSharp() // Initialize SkiaSharp
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkitMediaElement();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}