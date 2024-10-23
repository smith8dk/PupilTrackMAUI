using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;

namespace PupilTrack
{
    public partial class HGNTestPage : ContentPage
    {
        private VideoView videoView;
        private SKCanvasView canvasView;
        private string videoSource;

        public HGNTestPage()
        {
            InitializeComponent();

            // Initialize video view and canvas
            videoView = new VideoView
            {
                WidthRequest = 720,
                HeightRequest = 560
            };
            canvasView = new SKCanvasView
            {
                WidthRequest = 720,
                HeightRequest = 560
            };

            var filePicker = new Button
            {
                Text = "Upload Video"
            };
            filePicker.Clicked += OnUploadVideoClicked;

            var stackLayout = new StackLayout
            {
                Children = { filePicker, videoView, canvasView }
            };

            Content = stackLayout;

            canvasView.PaintSurface += OnCanvasViewPaintSurface;
        }

        private async void OnUploadVideoClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Videos,
                PickerTitle = "Select a video"
            });

            if (result != null)
            {
                videoSource = result.FullPath;
                videoView.Source = MediaSource.FromFile(videoSource);
                StartGrayscaleFeed();
            }
        }

        private void StartGrayscaleFeed()
        {
            videoView.Play();

            Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
            {
                canvasView.InvalidateSurface(); // Trigger canvas refresh
                return videoView.IsPlaying;
            });
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            var canvas = args.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // ROI dimensions
            float roiX = (float)(canvasView.Width * 0.15);
            float roiY = (float)(canvasView.Height * 0.15);
            float roiWidth = (float)(canvasView.Width * 0.6);
            float roiHeight = (float)(canvasView.Height * 0.6);

            // Draw the ROI rectangle
            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 2
            })
            {
                canvas.DrawRect(roiX, roiY, roiWidth, roiHeight, paint);
            }

            // Simulate grayscale conversion and draw the center of the eye (red dot)
            var centerEye = ApplyThreshold(canvas, roiX, roiY, roiWidth, roiHeight);

            if (centerEye != null)
            {
                using (var paint = new SKPaint { Color = SKColors.Red })
                {
                    canvas.DrawCircle(roiX + centerEye.Value.X, roiY + centerEye.Value.Y, 5, paint);
                }
            }
        }

        private SKPoint? ApplyThreshold(SKCanvas canvas, float roiX, float roiY, float roiWidth, float roiHeight)
        {
            // Simulate grayscale conversion and thresholding for the region of interest (ROI)
            // In real-world application, process the video frame's pixel data

            int threshold = 50; // Threshold value
            int darkPixelCount = 0;
            float totalX = 0;
            float totalY = 0;

            for (int x = (int)roiX; x < roiX + roiWidth; x++)
            {
                for (int y = (int)roiY; y < roiY + roiHeight; y++)
                {
                    // Dummy grayscale value for simulation (you'll need real pixel data)
                    float gray = (x + y) % 255;

                    if (gray < threshold)
                    {
                        totalX += x;
                        totalY += y;
                        darkPixelCount++;
                    }
                }
            }

            if (darkPixelCount > 0)
            {
                return new SKPoint(totalX / darkPixelCount, totalY / darkPixelCount);
            }

            return null;
        }
    }
}
