using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace PupilTrack
{
    public partial class HGNTestPage : ContentPage
    {
        // Flask server URL for video stabilization
        private const string ServerUrl = "http://192.168.1.40:5000";  // Replace with your Flask server URL

        // Local file path for the stabilized video
        private string localFilePath;

        public HGNTestPage()
        {
            InitializeComponent();
        }

        // Upload the video to the Flask server for stabilization
        private async Task UploadVideoAsync(string videoPath)
        {
            // Show the progress indicator while uploading
            ProgressIndicator.IsVisible = true;
            ProgressIndicator.IsRunning = true;

            // Read the video file as bytes
            var fileBytes = await File.ReadAllBytesAsync(videoPath);
            var videoContent = new ByteArrayContent(fileBytes);
            videoContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            using var client = new HttpClient();
            var formContent = new MultipartFormDataContent();
            formContent.Add(videoContent, "file", "video.mp4");  // Attach the file to the request

            try
            {
                // Send the POST request to the Flask server
                var response = await client.PostAsync($"{ServerUrl}/stabilize", formContent);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    // Extract the video URL from the server's response
                    var videoUrl = ExtractVideoUrlFromResponse(jsonResponse);
                    StatusLabel.Text = "Video stabilized successfully.";

                    // Download the stabilized video
                    await DownloadVideoAsync(videoUrl);
                }
                else
                {
                    StatusLabel.Text = "Failed to stabilize video.";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
            }
            finally
            {
                // Hide the progress indicator once the process is complete
                ProgressIndicator.IsVisible = false;
                ProgressIndicator.IsRunning = false;
            }
        }

        private string ExtractVideoUrlFromResponse(string jsonResponse)
        {
            // Clean up the response by removing escape characters and extra spaces
            jsonResponse = jsonResponse.Trim().Replace("\\n", "").Replace("\\", "");

            // Basic string manipulation to extract the URL
            var startIndex = jsonResponse.IndexOf("\"video_url\":\"") + 13;
            var endIndex = jsonResponse.IndexOf("\"", startIndex);

            // Return the URL from the extracted portion of the response
            return jsonResponse.Substring(startIndex, endIndex - startIndex);
        }

        // Download the stabilized video from the Flask server
        private async Task DownloadVideoAsync(string videoUrl)
        {
            using var client = new HttpClient();
            var videoData = await client.GetByteArrayAsync(videoUrl);

            // Save the downloaded video locally in the app's data directory
            localFilePath = Path.Combine(FileSystem.AppDataDirectory, "stabilized_video.mp4");
            await File.WriteAllBytesAsync(localFilePath, videoData);

            StatusLabel.Text = "Video downloaded successfully.";
            DownloadLabel.Text = $"Saved to: {localFilePath}";
            DownloadLabel.IsVisible = true;
            DownloadButton.IsVisible = true; // Show the download button
        }

        // Handle the download button click
        private void OnDownloadButtonClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(localFilePath))
            {
                // Share the stabilized video file
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

        // Handle video selection from the user (via file picker)
        private async void OnSelectVideoButtonClicked(object sender, EventArgs e)
        {
            // Open the file picker to select a video
            var fileResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select a video file",
                FileTypes = FilePickerFileType.Videos
            });

            if (fileResult != null)
            {
                // Start uploading the selected video
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
