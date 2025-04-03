from fastapi import FastAPI, UploadFile, File, Form, HTTPException
import socket
import json
import face_recognition
import numpy as np
from pathlib import Path
import io

app = FastAPI()

def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("8.8.8.8", 80))
        local_ip = s.getsockname()[0]
    except Exception:
        local_ip = "127.0.0.1"
    finally:
        s.close()
    return local_ip

def load_face_database():
    db_path = Path("faces_db/face_database.json")
    if not db_path.exists():
        # Create the directory if it doesn't exist
        Path("faces_db").mkdir(exist_ok=True)
        return {"faces": []}
    with open(db_path, "r") as f:
        return json.load(f)

def save_face_database(db):
    # Create the directory if it doesn't exist
    Path("faces_db").mkdir(exist_ok=True)
    with open("faces_db/face_database.json", "w") as f:
        json.dump(db, f, indent=4)

def is_similar_face(new_encoding, stored_faces, threshold=0.6):
    """Check if a face encoding is similar to any stored faces."""
    for face in stored_faces:
        stored_encoding = np.array(face["encoding"])
        # Compare faces and get the face distance (lower is better)
        face_distance = face_recognition.face_distance([stored_encoding], new_encoding)[0]
        if face_distance < threshold:  # Lower distance means more similar
            return True, face["name"]
    return False, None

@app.get("/")
async def root():
    return {"name": "MUF"}

@app.get("/ip")
async def get_ip():
    return {"ip": get_local_ip()}

@app.post("/add_face")
async def add_face(file: UploadFile = File(...), name: str = Form(...)):
    # Read the image file
    contents = await file.read()
    image = face_recognition.load_image_file(io.BytesIO(contents))
    
    # Find all face encodings in the image
    face_encodings = face_recognition.face_encodings(image)
    
    if not face_encodings:
        return {"error": "No face found in the image"}
    
    if len(face_encodings) > 1:
        return {"error": "Multiple faces found in the image. Please provide an image with only one face."}
    
    # Load the current database
    db = load_face_database()
    
    # Check if this face is already in the database
    is_duplicate, existing_name = is_similar_face(face_encodings[0], db["faces"])
    if is_duplicate:
        return {"error": f"This face appears to be already registered under the name: {existing_name}"}
    
    # Add the new face
    new_face = {
        "name": name,
        "encoding": face_encodings[0].tolist()  # Convert numpy array to list for JSON serialization
    }
    
    db["faces"].append(new_face)
    save_face_database(db)
    
    return {"message": f"Successfully added face for {name}"}

@app.delete("/delete_face/{name}")
async def delete_face(name: str):
    # Load the current database
    db = load_face_database()
    
    # Find and remove all faces with the given name
    initial_count = len(db["faces"])
    db["faces"] = [face for face in db["faces"] if face["name"] != name]
    deleted_count = initial_count - len(db["faces"])
    
    if deleted_count == 0:
        raise HTTPException(status_code=404, detail=f"No face found with name: {name}")
    
    # Save the updated database
    save_face_database(db)
    
    return {"message": f"Successfully deleted {deleted_count} face(s) for {name}"}

@app.post("/recognize_face")
async def recognize_face(file: UploadFile = File(...)):
    # Read the image file
    contents = await file.read()
    image = face_recognition.load_image_file(io.BytesIO(contents))
    
    # Find all face encodings in the image
    face_encodings = face_recognition.face_encodings(image)
    
    if not face_encodings:
        return {"error": "No face found in the image"}
    
    # Load the face database
    db = load_face_database()
    
    if not db["faces"]:
        return {"error": "No faces in the database"}
    
    # Compare the first face found with all faces in the database
    matches = []
    for stored_face in db["faces"]:
        stored_encoding = np.array(stored_face["encoding"])
        # Compare faces and get the face distance (lower is better)
        face_distance = face_recognition.face_distance([stored_encoding], face_encodings[0])[0]
        matches.append({
            "name": stored_face["name"],
            "confidence": 1 - float(face_distance)  # Convert distance to confidence score
        })
    
    # Sort matches by confidence
    matches.sort(key=lambda x: x["confidence"], reverse=True)
    
    if matches:
        return {
            "best_match": matches[0],
            "all_matches": matches
        }
    else:
        return {"error": "No matches found"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
    # print(get_local_ip())
