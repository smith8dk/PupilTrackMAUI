using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Layouts;

namespace PupilTrack
{
    public partial class HGNResultsPage : ContentPage
    {
        readonly ILogger logger;
        // Folder paths (update as needed)
        private const string ProcessedFolderPath = @"C:\Users\dswsm\source\repos\smith8dk\PupilTrackMAUI\PupilTrack\Resources\python\processed\";
        private const string SavedFolderPath = @"C:\Users\dswsm\source\repos\smith8dk\PupilTrackMAUI\PupilTrack\Resources\python\saved\";
        private double videoDuration = 1.0;

        public HGNResultsPage() : this(NullLogger<HGNResultsPage>.Instance) { }

        public HGNResultsPage(ILogger<HGNResultsPage> logger)
        {
            InitializeComponent();
            this.logger = logger;
            VideoPlayer.PropertyChanged += VideoPlayer_PropertyChanged;
            PositionSlider.SizeChanged += PositionSlider_SizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Task.Delay(5000);
            LoadLatestVideo();
            LoadMovementData();
        }

        private void LoadLatestVideo()
        {
            if (Directory.Exists(ProcessedFolderPath))
            {
                var files = Directory.GetFiles(ProcessedFolderPath, "*.mp4");
                if (files.Length > 0)
                {
                    Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    string latestFile = files[0];
                    VideoPlayer.Source = MediaSource.FromFile(latestFile);
                    logger.LogInformation("Loaded latest processed video: {File}", latestFile);
                }
                else
                {
                    logger.LogWarning("No processed video found in folder: {Folder}", ProcessedFolderPath);
                }
            }
            else
            {
                logger.LogWarning("Processed folder does not exist: {Folder}", ProcessedFolderPath);
            }
        }

        private void LoadMovementData()
        {
            if (Directory.Exists(SavedFolderPath))
            {
                var jsonFiles = Directory.GetFiles(SavedFolderPath, "*.json");
                if (jsonFiles.Length > 0)
                {
                    Array.Sort(jsonFiles, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    string latestJson = jsonFiles[0];
                    try
                    {
                        string jsonContent = File.ReadAllText(latestJson);
                        logger.LogInformation("Latest JSON file content:\n{JsonContent}", jsonContent);
                        using JsonDocument doc = JsonDocument.Parse(jsonContent);
                        if (doc.RootElement.TryGetProperty("sudden_movements", out JsonElement movementArray))
                        {
                            int count = movementArray.GetArrayLength();
                            MovementCounterLabel.Text = $"Sudden Movements: {count}";

                            string timestampsText = "Timestamps:\n";
                            double[] timestamps = new double[count];
                            int index = 0;
                            foreach (JsonElement element in movementArray.EnumerateArray())
                            {
                                double t = element.GetDouble();
                                timestampsText += $"{t:F2} s\n";
                                timestamps[index++] = t;
                            }
                            MovementTimestampsLabel.Text = timestampsText;
                            logger.LogInformation("Loaded movement data from {JsonFile}: {Count} movements", latestJson, count);
                            UpdateSliderMarkers(timestamps);
                        }
                        else
                        {
                            MovementCounterLabel.Text = "Sudden Movements: 0";
                            MovementTimestampsLabel.Text = "Timestamps: None";
                            logger.LogWarning("No 'sudden_movements' property found in JSON file {JsonFile}", latestJson);
                        }
                    }
                    catch (Exception ex)
                    {
                        MovementCounterLabel.Text = "Sudden Movements: 0";
                        MovementTimestampsLabel.Text = "Timestamps: None";
                        logger.LogError(ex, "Failed to parse JSON file {JsonFile}", latestJson);
                    }
                }
                else
                {
                    MovementCounterLabel.Text = "Sudden Movements: 0";
                    MovementTimestampsLabel.Text = "Timestamps: None";
                    logger.LogWarning("No JSON result files found in folder: {Folder}", SavedFolderPath);
                }
            }
            else
            {
                MovementCounterLabel.Text = "Sudden Movements: 0";
                MovementTimestampsLabel.Text = "Timestamps: None";
                logger.LogWarning("Saved folder does not exist: {Folder}", SavedFolderPath);
            }
        }

        private void UpdateSliderMarkers(double[] timestamps)
        {
            MarkerContainer.Children.Clear();
            if (PositionSlider.Width <= 0 || PositionSlider.Maximum <= 0)
                return;

            double sliderWidth = PositionSlider.Width;
            double maxTime = PositionSlider.Maximum;

            foreach (double t in timestamps)
            {
                double relativePosition = t / maxTime;
                // Create a small yellow BoxView marker.
                BoxView marker = new BoxView
                {
                    Color = Colors.Yellow,
                    WidthRequest = 2,
                    HeightRequest = 10,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };

                // Place the marker relative to the slider's width.
                AbsoluteLayout.SetLayoutBounds(marker, new Rect(relativePosition, 0.5, 0.01, 1));
                AbsoluteLayout.SetLayoutFlags(marker, AbsoluteLayoutFlags.All);
                MarkerContainer.Children.Add(marker);
            }
        }

        private void PositionSlider_SizeChanged(object sender, EventArgs e)
        {
            // Reload movement data to update markers.
            LoadMovementData();
        }

        void VideoPlayer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MediaElement.DurationProperty.PropertyName)
            {
                videoDuration = VideoPlayer.Duration.TotalSeconds;
                PositionSlider.Maximum = videoDuration;
            }
        }

        void OnMediaOpened(object? sender, EventArgs e) =>
            logger.LogInformation("Media opened.");

        void OnMediaFailed(object? sender, MediaFailedEventArgs e) =>
            logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

        void OnMediaEnded(object? sender, EventArgs e) =>
            logger.LogInformation("Media ended.");

        void OnPositionChanged(object? sender, EventArgs e)
        {
            logger.LogInformation("Position changed to {Position}", VideoPlayer.Position);
            PositionSlider.Value = VideoPlayer.Position.TotalSeconds;
        }

        void OnSeekCompleted(object? sender, EventArgs e) =>
            logger.LogInformation("Seek completed.");

        async void Slider_DragCompleted(object? sender, EventArgs e)
        {
            if (sender is Slider slider)
            {
                double newValue = slider.Value;
                await VideoPlayer.SeekTo(TimeSpan.FromSeconds(newValue), CancellationToken.None);
            }
        }

        void Slider_DragStarted(object sender, EventArgs e)
        {
            VideoPlayer.Pause();
            logger.LogInformation("Slider drag started.");
        }

        void OnPlayClicked(object? sender, EventArgs e) => VideoPlayer.Play();
        void OnPauseClicked(object? sender, EventArgs e) => VideoPlayer.Pause();
        void OnStopClicked(object? sender, EventArgs e) => VideoPlayer.Stop();
        void OnMuteClicked(object? sender, EventArgs e) => VideoPlayer.ShouldMute = !VideoPlayer.ShouldMute;

        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            VideoPlayer.Stop();
            VideoPlayer.Handler?.DisconnectHandler();
        }
    }
}
