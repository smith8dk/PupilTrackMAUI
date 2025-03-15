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
app.config['UPLOAD_FOLDER'] = 'uploads'
app.config['PROCESSED_FOLDER'] = 'processed'
app.config['RESULTS_FOLDER'] = 'results'

# Ensure folders exist
os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)
os.makedirs(app.config['PROCESSED_FOLDER'], exist_ok=True)
os.makedirs(app.config['RESULTS_FOLDER'], exist_ok=True)

def stabilize_and_grayscale(input_path, output_path, zoom_factor=1.2, roi_width=900, roi_height=300, movement_threshold=30, cooldown_time=0.3):
    """
    Stabilize a video, convert it to grayscale, automatically threshold using Otsu's method with reversed colors,
    and isolate the largest cluster within an elliptical ROI with a zoom effect.
    
    A red dot is drawn at the centroid of the largest cluster.
    Sudden movement is detected when the distance between centroids in consecutive frames exceeds movement_threshold.
    A cooldown period is enforced before detecting another movement.
    
    Returns a list of timestamps (in seconds) when sudden movement was detected.
    """
    stabilizer = VidStab()
    temp_stabilized_path = "temp_stabilized.mp4"

    # Stabilize the video.
    stabilizer.stabilize(input_path=input_path, output_path=temp_stabilized_path)

    cap = cv2.VideoCapture(temp_stabilized_path)
    fourcc = cv2.VideoWriter_fourcc(*'avc1')
    fps = int(cap.get(cv2.CAP_PROP_FPS))
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    out = cv2.VideoWriter(output_path, fourcc, fps, (width, height), isColor=True)

    sudden_movements = []  # List to store timestamps of sudden movements.
    previous_centroid = None
    last_movement_time = -cooldown_time  # Initialize to allow immediate first detection
    frame_index = 0

    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            break

        current_timestamp = frame_index / fps

        # Zoom the frame.
        zoomed_frame = cv2.resize(frame, None, fx=zoom_factor, fy=zoom_factor, interpolation=cv2.INTER_LINEAR)
        center_x, center_y = zoomed_frame.shape[1] // 2, zoomed_frame.shape[0] // 2

        # Crop the center of the zoomed frame.
        crop_width = width
        crop_height = height
        cropped_frame = zoomed_frame[center_y - crop_height // 2: center_y + crop_height // 2,
                                      center_x - crop_width // 2: center_x + crop_width // 2]

        # Convert to grayscale.
        gray_frame = cv2.cvtColor(cropped_frame, cv2.COLOR_BGR2GRAY)

        # Automatic thresholding using Otsu's method.
        _, thresholded_frame = cv2.threshold(gray_frame, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

        # Invert colors.
        inverted_frame = cv2.bitwise_not(thresholded_frame)

        # Create an empty mask.
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

        # Create a white frame.
        largest_cluster_frame = 255 * np.ones_like(gray_frame)

        # Draw the largest contour filled with black.
        cv2.drawContours(largest_cluster_frame, [largest_contour], -1, 0, thickness=cv2.FILLED)

        # Convert to BGR so we can overlay a red dot.
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
                if distance > movement_threshold and (current_timestamp - last_movement_time) >= cooldown_time:
                    sudden_movements.append(current_timestamp)
                    last_movement_time = current_timestamp  # Update last movement time
            previous_centroid = current_centroid

            # Draw a red dot at the centroid.
            cv2.circle(color_frame, current_centroid, 5, (0, 0, 255), -1)

        out.write(color_frame)
        frame_index += 1

    cap.release()
    out.release()
    os.remove(temp_stabilized_path)
    
    return sudden_movements
