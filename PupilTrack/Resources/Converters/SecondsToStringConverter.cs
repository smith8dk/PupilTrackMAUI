using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace PupilTrack.Converters
{
    public class SecondsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "00:00";

            TimeSpan timeSpan;

            // Check if the value is a TimeSpan or a numeric type (representing seconds)
            if (value is TimeSpan ts)
            {
                timeSpan = ts;
            }
            else if (value is double d)
            {
                timeSpan = TimeSpan.FromSeconds(d);
            }
            else if (value is int i)
            {
                timeSpan = TimeSpan.FromSeconds(i);
            }
            else
            {
                return value.ToString();
            }

            // Use hh:mm:ss if duration is an hour or more, else use mm:ss
            return timeSpan.TotalHours >= 1
                ? timeSpan.ToString(@"hh\:mm\:ss")
                : timeSpan.ToString(@"mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack isn't needed for display purposes
            throw new NotImplementedException();
        }
    }
}
