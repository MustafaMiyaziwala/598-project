import cv2

# Stream URL with authentication
username = "DUDE-HL2-01"
password = "DUDE-HL2-01"
ip = "35.2.191.18"

stream_url = f"https://{username}:{password}@{ip}/api/holographic/stream/live_high.mp4?holo=true&pv=true&mic=false&loopback=true&RenderFromCamera=true"  # Adjust path_to_stream

# Open video stream
cap = cv2.VideoCapture(stream_url)

if not cap.isOpened():
    print("Error: Could not open video stream")
    exit()

while True:
    ret, frame = cap.read()
    if not ret:
        print("Error: Failed to retrieve frame")
        break

    cv2.imshow("Video Stream", frame)

    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cap.release()
cv2.destroyAllWindows()