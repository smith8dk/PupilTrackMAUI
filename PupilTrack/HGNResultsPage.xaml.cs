using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core.Primitives;

namespace PupilTrack
{
    public partial class HGNResultsPage : ContentPage
    {
        readonly ILogger logger;

        // Constants for alternative sources remain (if needed)
        const string botImageUrl = "https://lh3.googleusercontent.com/pw/AP1GczNRrebWCJvfdIau1EbsyyYiwAfwHS0JXjbioXvHqEwYIIdCzuLodQCZmA57GADIo5iB3yMMx3t_vsefbfoHwSg0jfUjIXaI83xpiih6d-oT7qD_slR0VgNtfAwJhDBU09kS5V2T5ZML-WWZn8IrjD4J-g=w1792-h1024-s-no-gm";
        const string hlsStreamTestUrl = "https://mtoczko.github.io/hls-test-streams/test-gap/playlist.m3u8";
        const string hal9000AudioUrl = "https://github.com/prof3ssorSt3v3/media-sample-files/raw/master/hal-9000.mp3";

        // Folder where processed videos are stored.
        private const string ProcessedFolderPath = @"C:\Users\dswsm\Downloads\PupilTrackMAUI-master\PupilTrackMAUI-master\PupilTrack\Resources\python\processed";

        // Parameterless constructor uses a default (null) logger.
        public HGNResultsPage() : this(NullLogger<HGNResultsPage>.Instance)
        {
        }

        public HGNResultsPage(ILogger<HGNResultsPage> logger)
        {
            InitializeComponent();
            this.logger = logger;
            VideoPlayer.PropertyChanged += VideoPlayer_PropertyChanged;
        }

        // When the page appears, wait 5 seconds and load the latest processed video.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Task.Delay(5000);
            LoadLatestVideo();
        }

        // Checks the processed folder for the most recent .mp4 file and sets it as the source.
        private void LoadLatestVideo()
        {
            if (Directory.Exists(ProcessedFolderPath))
            {
                var files = Directory.GetFiles(ProcessedFolderPath, "*.mp4");
                if (files.Length > 0)
                {
                    // Sort files descending by last write time.
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

        void VideoPlayer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == MediaElement.DurationProperty.PropertyName)
            {
                logger.LogInformation("Duration: {NewDuration}", VideoPlayer.Duration);
                PositionSlider.Maximum = VideoPlayer.Duration.TotalSeconds;
            }
        }

        void OnMediaOpened(object? sender, EventArgs e) =>
            logger.LogInformation("Media opened.");

        void OnMediaFailed(object? sender, MediaFailedEventArgs e) =>
            logger.LogInformation("Media failed. Error: {ErrorMessage}", e.ErrorMessage);

        void OnMediaEnded(object? sender, EventArgs e) =>
            logger.LogInformation("Media ended.");

        // Update slider based on VideoPlayer.Position.
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

        // Playback control event handlers.
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
