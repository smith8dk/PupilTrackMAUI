using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public HGNTestPage()
        {
            InitializeComponent();
        }

        // Helper methods for showing/hiding the loading overlay.
        private void ShowLoadingScreen()
        {
            // Hide the main UI content and show the overlay.
            MainContent.IsVisible = false;
            LoadingOverlay.IsVisible = true;
        }

        private void HideLoadingScreen()
        {
            // Hide the overlay and show the main UI again.
            LoadingOverlay.IsVisible = false;
            MainContent.IsVisible = true;
        }

        private void MyCamera_MediaCaptured(object sender, EventArgs e)
        {
            DisplayAlert("Media Captured", "A photo or video has been captured.", "OK");
        }

        private async void OnRecordButtonClicked(object sender, EventArgs e)
        {
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
                        // Save the recorded file in the app's data directory.
                        recordedVideoPath = Path.Combine(FileSystem.AppDataDirectory, file.FileName);
                        using var stream = await file.OpenReadAsync();
                        using var newStream = File.OpenWrite(recordedVideoPath);
                        await stream.CopyToAsync(newStream);

                        await DisplayAlert("Recording Complete", $"Video saved at:\n{recordedVideoPath}", "OK");

                        // Display the recorded video (optional).
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
            // Convert the local file path to a proper file URI.
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

        // Displays the processed video (directly from the local directory) using a WebView.
        private void ShowProcessedVideo(string videoPath)
        {
            CameraViewControl.IsVisible = false;
            // Convert the local file path to a file URI.
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
            // Show the loading overlay.
            ShowLoadingScreen();

            // Show the progress indicator.
            ProgressIndicator.IsVisible = true;
            ProgressIndicator.IsRunning = true;

            // Read the video file as bytes.
            var fileBytes = await File.ReadAllBytesAsync(videoPath);
            var videoContent = new ByteArrayContent(fileBytes);
            videoContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            using var client = new HttpClient();
            var formContent = new MultipartFormDataContent();
            formContent.Add(videoContent, "file", "video.mp4");

            try
            {
                // Send the POST request to the Flask server.
                var response = await client.PostAsync($"{ServerUrl}/stabilize", formContent);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    // Extract the processed video URL from the response.
                    var videoUrl = ExtractVideoUrlFromResponse(jsonResponse);
                    StatusLabel.Text = "Video stabilized successfully.";

                    // Download the processed video.
                    await DownloadVideoAsync(videoUrl);
                }
                else
                {
                    StatusLabel.Text = "Failed to stabilize video.";
                    HideLoadingScreen();
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
                HideLoadingScreen();
            }
            finally
            {
                ProgressIndicator.IsVisible = false;
                ProgressIndicator.IsRunning = false;
            }
        }

        // Extracts the processed video URL from the JSON response.
        private string ExtractVideoUrlFromResponse(string jsonResponse)
        {
            jsonResponse = jsonResponse.Trim().Replace("\\n", "").Replace("\\", "");
            var startIndex = jsonResponse.IndexOf("\"video_url\":\"") + 13;
            var endIndex = jsonResponse.IndexOf("\"", startIndex);
            return jsonResponse.Substring(startIndex, endIndex - startIndex);
        }

        // Downloads the processed video from the Flask server.
        private async Task DownloadVideoAsync(string videoUrl)
        {
            using var client = new HttpClient();
            var videoData = await client.GetByteArrayAsync(videoUrl);

            // Save the processed video locally.
            localFilePath = Path.Combine(FileSystem.AppDataDirectory, "stabilized_video.mp4");
            await File.WriteAllBytesAsync(localFilePath, videoData);

            StatusLabel.Text = "Video downloaded and processed successfully.";

            // Display the processed video from the local file.
            ShowProcessedVideo(localFilePath);

            // Hide the loading overlay.
            HideLoadingScreen();
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
            else
            {
                StatusLabel.Text = "No video available to download.";
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
                StatusLabel.Text = "Uploading video...";
                await UploadVideoAsync(fileResult.FullPath);
            }
            else
            {
                StatusLabel.Text = "No video selected.";
            }
        }
    }
}
