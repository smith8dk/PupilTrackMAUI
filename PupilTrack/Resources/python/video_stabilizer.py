from flask import Flask, request, jsonify
from werkzeug.utils import secure_filename
from vidstab import VidStab
from flask_cors import CORS
import os
import cv2
import time
import numpy as np

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes
app.config['UPLOAD_FOLDER'] = 'uploads'
app.config['PROCESSED_FOLDER'] = 'processed'

# Ensure folders exist
os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)
os.makedirs(app.config['PROCESSED_FOLDER'], exist_ok=True)

def stabilize_and_grayscale(input_path, output_path, threshold=105, zoom_factor=1.2, roi_width=900, roi_height=300):
    """Stabilize a video, convert it to grayscale, apply thresholding with reversed colors,
    and keep only the largest cluster within a sideways ellipse ROI with zoom effect."""
    stabilizer = VidStab()
    temp_stabilized_path = "temp_stabilized.mp4"

    # Stabilize the video first
    stabilizer.stabilize(input_path=input_path, output_path=temp_stabilized_path)

    # Open the stabilized video and process it for grayscale with thresholding
    cap = cv2.VideoCapture(temp_stabilized_path)
    # Change the codec to H.264 (avc1) for better compatibility on mobile devices
    fourcc = cv2.VideoWriter_fourcc(*'avc1')  # Changed from 'mp4v' to 'avc1'
    fps = int(cap.get(cv2.CAP_PROP_FPS))
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    out = cv2.VideoWriter(output_path, fourcc, fps, (width, height), isColor=False)

    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            break

        # Zooming in by 20% - Resize the frame
        zoomed_frame = cv2.resize(frame, None, fx=zoom_factor, fy=zoom_factor, interpolation=cv2.INTER_LINEAR)

        # Get the center of the zoomed frame
        center_x, center_y = zoomed_frame.shape[1] // 2, zoomed_frame.shape[0] // 2

        # Calculate the region to crop (crop the center of the zoomed image to maintain the original aspect ratio)
        crop_width = width
        crop_height = height
        cropped_frame = zoomed_frame[center_y - crop_height // 2 : center_y + crop_height // 2,
                                      center_x - crop_width // 2 : center_x + crop_width // 2]

        # Convert to grayscale
        gray_frame = cv2.cvtColor(cropped_frame, cv2.COLOR_BGR2GRAY)

        # Apply thresholding to get the correct black and white coloring
        _, thresholded_frame = cv2.threshold(gray_frame, threshold, 255, cv2.THRESH_BINARY)

        # Invert the colors to reverse black and white
        inverted_frame = cv2.bitwise_not(thresholded_frame)

        # Create a blank mask with the same size as the frame
        mask = np.zeros_like(inverted_frame)

        # Adjust ROI dimensions
        adjusted_roi_width = int(roi_width * 0.7)  # Make the ROI 10% less wide
        adjusted_roi_height = int(roi_height * 1.2)  # Make the ROI 20% higher

        # ROI logic: Keep the original center
        roi_x = width // 2
        roi_y = height // 2
        cv2.ellipse(mask, (roi_x, roi_y), (adjusted_roi_width // 2, adjusted_roi_height // 2), 0, 0, 360, 255, -1)

        # Apply the mask to the inverted frame (only keep pixels inside the ellipse)
        masked_frame = cv2.bitwise_and(inverted_frame, inverted_frame, mask=mask)

        # Find contours (connected components) in the masked image
        contours, _ = cv2.findContours(masked_frame, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

        # If no contours are found, skip this frame
        if len(contours) == 0:
            continue

        # Find the largest contour by area
        largest_contour = max(contours, key=cv2.contourArea)

        # Create a blank frame (white) to draw the largest contour
        largest_cluster_frame = 255 * np.ones_like(gray_frame)

        # Fill the largest contour with black (or close to black) on the blank frame
        cv2.drawContours(largest_cluster_frame, [largest_contour], -1, (0), thickness=cv2.FILLED)

        # Write the frame with the largest black cluster
        out.write(largest_cluster_frame)

    cap.release()
    out.release()

    # Remove the temporary stabilized video
    os.remove(temp_stabilized_path)

@app.route('/stabilize', methods=['POST'])
def stabilize_video():
    # Check if video file is provided
    if 'file' not in request.files:
        return jsonify({'error': 'No file provided'}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({'error': 'No selected file'}), 400

    # Save uploaded video
    filename = secure_filename(file.filename)
    input_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
    file.save(input_path)

    # Generate a unique output filename based on the original video name and timestamp
    timestamp = int(time.time())  # Using the current timestamp to create a unique filename
    base_name, _ = os.path.splitext(filename)
    processed_filename = f'{base_name}_{timestamp}_processed.mp4'
    processed_path = os.path.join(app.config['PROCESSED_FOLDER'], processed_filename)

    # Process the video (stabilization + grayscale with thresholding, largest black cluster, and zoom)
    stabilize_and_grayscale(input_path, processed_path)

    # Return the URL of the processed video
    processed_video_url = f"http://localhost:5000/{app.config['PROCESSED_FOLDER']}/{processed_filename}"
    return jsonify({'video_url': processed_video_url})

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
