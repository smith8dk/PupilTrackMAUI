using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Core.Primitives; // Ensure you reference the correct namespace for MediaElementState

namespace PupilTrack.Converters
{
    public class PlayPauseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Instead of casting to bool, cast to MediaElementState
            if (value is MediaElementState state)
            {
                // Return the pause icon if the video is playing, otherwise the play icon.
                return state == MediaElementState.Playing ? "pause_icon.png" : "play_icon.png";
            }
            // Fallback icon
            return "play_icon.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
