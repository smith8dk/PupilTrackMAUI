from flask import Flask, request, jsonify
from werkzeug.utils import secure_filename
from vidstab import VidStab
from flask_cors import CORS
import os
import cv2
import time
import numpy as np
import math
import json

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

# Set folder paths relative to this script's directory.
base_dir = os.path.dirname(os.path.abspath(__file__))
app.config['UPLOAD_FOLDER'] = os.path.join(base_dir, "uploads")
app.config['PROCESSED_FOLDER'] = os.path.join(base_dir, "processed")
saved_folder = os.path.join(base_dir, "saved")

# Ensure folders exist.
os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)
os.makedirs(app.config['PROCESSED_FOLDER'], exist_ok=True)
os.makedirs(saved_folder, exist_ok=True)

def stabilize_and_detect_movements(input_path, output_path, zoom_factor=1.2, roi_width=900, roi_height=300, movement_threshold=30):
    """
    Stabilize a video, convert it to grayscale, automatically threshold using Otsu's method with reversed colors,
    and isolate the largest cluster within an elliptical ROI with zoom effect.
    
    Additionally, detect "sudden" movements by calculating the centroid of the largest cluster for each frame.
    If the centroid moves more than 'movement_threshold' pixels compared to the previous frame, record the timestamp.
    
    Returns a list of timestamps (in seconds) when sudden movement was detected.
    """
    stabilizer = VidStab()
    temp_stabilized_path = os.path.join(os.path.dirname(input_path), "temp_stabilized.mp4")
    
    # Stabilize the video.
    stabilizer.stabilize(input_path=input_path, output_path=temp_stabilized_path)
    
    cap = cv2.VideoCapture(temp_stabilized_path)
    fourcc = cv2.VideoWriter_fourcc(*'avc1')  # H.264 codec
    fps = int(cap.get(cv2.CAP_PROP_FPS))
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    # We'll output color frames so we can overlay a red dot.
    out = cv2.VideoWriter(output_path, fourcc, fps, (width, height), isColor=True)
    
    sudden_movements = []
    previous_centroid = None
    frame_index = 0
    
    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            break
        
        current_timestamp = frame_index / fps
        
        # Zoom the frame.
        zoomed_frame = cv2.resize(frame, None, fx=zoom_factor, fy=zoom_factor, interpolation=cv2.INTER_LINEAR)
        center_x, center_y = zoomed_frame.shape[1] // 2, zoomed_frame.shape[0] // 2
        
        # Crop the center.
        crop_width = width
        crop_height = height
        cropped_frame = zoomed_frame[center_y - crop_height // 2 : center_y + crop_height // 2,
                                      center_x - crop_width // 2 : center_x + crop_width // 2]
        
        # Convert to grayscale.
        gray_frame = cv2.cvtColor(cropped_frame, cv2.COLOR_BGR2GRAY)
        
        # Automatic thresholding using Otsu's method.
        _, thresholded_frame = cv2.threshold(gray_frame, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
        
        # Invert colors.
        inverted_frame = cv2.bitwise_not(thresholded_frame)
        
        # Create a blank mask.
        mask = np.zeros_like(inverted_frame)
        
        # Adjust ROI dimensions.
        adjusted_roi_width = int(roi_width * 0.7)
        adjusted_roi_height = int(roi_height * 1.2)
        
        # Define an elliptical ROI at the center.
        roi_x = width // 2
        roi_y = height // 2
        cv2.ellipse(mask, (roi_x, roi_y), (adjusted_roi_width // 2, adjusted_roi_height // 2), 0, 0, 360, 255, -1)
        
        # Apply the mask.
        masked_frame = cv2.bitwise_and(inverted_frame, inverted_frame, mask=mask)
        
        # Find contours.
        contours, _ = cv2.findContours(masked_frame, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        if len(contours) == 0:
            frame_index += 1
            continue
        
        # Get the largest contour.
        largest_contour = max(contours, key=cv2.contourArea)
        
        # Create a blank white frame.
        largest_cluster_frame = 255 * np.ones_like(gray_frame)
        
        # Draw the largest contour filled with black.
        cv2.drawContours(largest_cluster_frame, [largest_contour], -1, 0, thickness=cv2.FILLED)
        
        # Convert the frame to color so we can overlay a red dot.
        color_frame = cv2.cvtColor(largest_cluster_frame, cv2.COLOR_GRAY2BGR)
        
        # Compute centroid of the largest contour.
        M = cv2.moments(largest_contour)
        if M["m00"] != 0:
            cx = int(M["m10"] / M["m00"])
            cy = int(M["m01"] / M["m00"])
            current_centroid = (cx, cy)
            
            if previous_centroid is not None:
                dx = current_centroid[0] - previous_centroid[0]
                dy = current_centroid[1] - previous_centroid[1]
                distance = math.sqrt(dx*dx + dy*dy)
                if distance > movement_threshold:
                    sudden_movements.append(current_timestamp)
            previous_centroid = current_centroid
            
            # Draw a red dot at the centroid.
            cv2.circle(color_frame, current_centroid, 5, (0, 0, 255), -1)
        
        out.write(color_frame)
        frame_index += 1
    
    cap.release()
    out.release()
    os.remove(temp_stabilized_path)
    
    return sudden_movements

@app.route('/stabilize', methods=['POST'])
def stabilize_video():
    # Check if a video file is provided.
    if 'file' not in request.files:
        return jsonify({'error': 'No file provided'}), 400
    
    file = request.files['file']
    if file.filename == '':
        return jsonify({'error': 'No selected file'}), 400
    
    # Save the uploaded video.
    filename = secure_filename(file.filename)
    input_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
    file.save(input_path)
    print(f"Uploaded video saved to: {input_path}")
    
    # Generate unique output filenames.
    timestamp = int(time.time())
    base_name, _ = os.path.splitext(filename)
    processed_filename = f"{base_name}_{timestamp}_processed.mp4"
    processed_path = os.path.join(app.config['PROCESSED_FOLDER'], processed_filename)
    
    # Process the video and detect sudden movement timestamps.
    movement_timestamps = stabilize_and_detect_movements(
        input_path,
        processed_path,
        zoom_factor=1.2,
        roi_width=900,
        roi_height=300,
        movement_threshold=30
    )
    
    print(f"Stabilized video saved to: {processed_path}")
    
    # Create a JSON results object.
    results = {
        "video_url": f"http://localhost:5000/{app.config['PROCESSED_FOLDER']}/{processed_filename}",
        "sudden_movements": movement_timestamps
    }
    
    # Save the JSON file in the 'saved' folder.
    saved_folder = os.path.join(base_dir, "saved")
    os.makedirs(saved_folder, exist_ok=True)
    json_filename = f"{base_name}_{timestamp}_results.json"
    json_path = os.path.join(saved_folder, json_filename)
    with open(json_path, "w") as f:
        json.dump(results, f)
    
    json_file_url = f"http://localhost:5000/saved/{json_filename}"
    print(f"Results JSON saved to: {json_path}")
    return jsonify({"results_url": json_file_url})

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
