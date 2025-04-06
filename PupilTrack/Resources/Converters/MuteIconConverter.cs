using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace PupilTrack.Converters
{
    public class MuteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMuted = (bool)value;
            return isMuted ? "unmute_icon.png" : "mute_icon.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
