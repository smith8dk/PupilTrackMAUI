using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices; // For DeviceInfo and DevicePlatform
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PupilTrack
{
    public partial class HGNTestPage : ContentPage
    {
        // Flask server URL for video stabilization
        private const string ServerUrl = "http://192.168.1.40:5000";  // Replace with your Flask server URL

        // Local file path for the stabilized video
        private string localFilePath;
        private bool isRecording = false;
        private string recordedVideoPath;

        // Folder where processed videos are stored (Adjust this path if needed)
        private const string ProcessedFolderPath = @"C:\Users\dswsm\source\repos\smith8dk\PupilTrackMAUI\PupilTrack\Resources\python\processed\";

        public HGNTestPage()
        {
            InitializeComponent();
        }

        // Helper methods for showing/hiding the loading overlay.
        private void ShowLoadingScreen()
        {
            MainContent.IsVisible = false;
            LoadingOverlay.IsVisible = true;
        }

        private void HideLoadingScreen()
        {
            LoadingOverlay.IsVisible = false;
            MainContent.IsVisible = true;
        }

        private void MyCamera_MediaCaptured(object sender, EventArgs e)
        {
            DisplayAlert("Media Captured", "A photo or video has been captured.", "OK");
        }

        private async void OnRecordButtonClicked(object sender, EventArgs e)
        {
            // Check if the app is running on WinUI.
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                await DisplayAlert("Recording Disabled",
                    "Recording via external camera is disabled on this platform. Please use the integrated CameraView.",
                    "OK");
                return;
            }

            if (!isRecording)
            {
                isRecording = true;
                RecordButton.BackgroundColor = Colors.Gray;
                await StartRecording();
            }
            else
            {
                isRecording = false;
                RecordButton.BackgroundColor = Colors.Red;
                await StopRecording();
            }
        }

        private async Task StartRecording()
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                try
                {
                    var file = await MediaPicker.Default.CaptureVideoAsync();
                    if (file != null)
                    {
                        recordedVideoPath = Path.Combine(FileSystem.AppDataDirectory, file.FileName);
                        using var stream = await file.OpenReadAsync();
                        using var newStream = File.OpenWrite(recordedVideoPath);
                        await stream.CopyToAsync(newStream);

                        await DisplayAlert("Recording Complete", $"Video saved at:\n{recordedVideoPath}", "OK");

                        // Optionally display the recorded video.
                        ShowRecordedVideo(recordedVideoPath);

                        // Start processing/uploading the video.
                        await UploadVideoAsync(recordedVideoPath);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Recording failed: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Video recording is not supported on this device.", "OK");
            }
        }

        private async Task StopRecording()
        {
            await DisplayAlert("Recording Stopped", "Your video has been saved.", "OK");
        }

        // Displays the recorded video using a WebView.
        private void ShowRecordedVideo(string videoPath)
        {
            CameraViewControl.IsVisible = false;
            string fileUri = new Uri(videoPath).AbsoluteUri;

            string htmlString = $@"
            <html>
                <head>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin:0;padding:0;overflow:hidden;background:black;'>
                    <video width='100%' height='100%' controls autoplay>
                        <source src='{fileUri}' type='video/mp4'>
                        Your browser does not support the video tag.
                    </video>
                </body>
            </html>";

            VideoWebView.Source = new HtmlWebViewSource { Html = htmlString };
            VideoWebView.IsVisible = true;
        }

        // Uploads the video to the Flask server for stabilization and processing.
        private async Task UploadVideoAsync(string videoPath)
        {
            ShowLoadingScreen();
            ProgressIndicator.IsVisible = true;
            ProgressIndicator.IsRunning = true;

            var fileBytes = await File.ReadAllBytesAsync(videoPath);
            var videoContent = new ByteArrayContent(fileBytes);
            videoContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            using var client = new HttpClient();
            var formContent = new MultipartFormDataContent();
            formContent.Add(videoContent, "file", "video.mp4");

            try
            {
                var response = await client.PostAsync($"{ServerUrl}/stabilize", formContent);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var videoUrl = ExtractVideoUrlFromResponse(jsonResponse);
                    await DownloadVideoAsync(videoUrl);
                }
                else
                {
                    // If response is not successful, log error.
                }
            }
            catch (Exception ex)
            {
                // Log error if necessary.
            }
            finally
            {
                ProgressIndicator.IsVisible = false;
                ProgressIndicator.IsRunning = false;
            }

            // Wait 5 seconds for the video to fully process.
            if (await CheckForProcessedVideoAsync())
            {
                await Navigation.PushAsync(new HGNResultsPage());
            }
            else
            {
                HideLoadingScreen();
                await DisplayAlert("Error", "Processed video not found.", "OK");
            }
        }

        // Extracts the processed video URL from the JSON response.
        private string ExtractVideoUrlFromResponse(string jsonResponse)
        {
            jsonResponse = jsonResponse.Trim().Replace("\\n", "").Replace("\\", "");
            var startIndex = jsonResponse.IndexOf("\"video_url\":\"") + 13;
            var endIndex = jsonResponse.IndexOf("\"", startIndex);
            return jsonResponse[startIndex..endIndex];
        }

        // Downloads the processed video from the Flask server.
        private async Task DownloadVideoAsync(string videoUrl)
        {
            using var client = new HttpClient();
            var videoData = await client.GetByteArrayAsync(videoUrl);

            localFilePath = Path.Combine(FileSystem.AppDataDirectory, "stabilized_video.mp4");
            await File.WriteAllBytesAsync(localFilePath, videoData);

            // Optionally display the processed video.
            ShowProcessedVideo(localFilePath);
            HideLoadingScreen();
        }

        // Displays the processed video using a WebView.
        private void ShowProcessedVideo(string videoPath)
        {
            CameraViewControl.IsVisible = false;
            string fileUri = new Uri(videoPath).AbsoluteUri;

            string htmlString = $@"
            <html>
                <head>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin:0;padding:0;overflow:hidden;background:black;'>
                    <video width='100%' height='100%' controls autoplay>
                        <source src='{fileUri}' type='video/mp4'>
                        Your browser does not support the video tag.
                    </video>
                </body>
            </html>";

            VideoWebView.Source = new HtmlWebViewSource { Html = htmlString };
            VideoWebView.IsVisible = true;
        }

        // Shares the processed video file when the download button is clicked.
        private void OnDownloadButtonClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(localFilePath))
            {
                Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Download Stabilized Video",
                    File = new ShareFile(localFilePath)
                });
            }
        }

        // Handles video selection from the user (via file picker) and starts processing.
        private async void OnSelectVideoButtonClicked(object sender, EventArgs e)
        {
            var fileResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select a video file",
                FileTypes = FilePickerFileType.Videos
            });

            if (fileResult != null)
            {
                await UploadVideoAsync(fileResult.FullPath);
            }
        }

        // Checks if a processed video exists in the processed folder.
        private async Task<bool> CheckForProcessedVideoAsync()
        {
            await Task.Delay(5000);

            if (Directory.Exists(ProcessedFolderPath))
            {
                var files = Directory.GetFiles(ProcessedFolderPath, "*.mp4");
                if (files.Length > 0)
                {
                    Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    localFilePath = files[0];
                    return true;
                }
            }
            return false;
        }
    }
}
