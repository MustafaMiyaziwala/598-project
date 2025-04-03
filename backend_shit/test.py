import requests
import json
import os

def test_add_face(name: str):
    """Test adding a face to the database"""
    url = "http://localhost:8000/add_face"
    
    # Prepare the files and form data
    files = {
        "file": (f"{name}.jpeg", open(f"faces/{name}.jpeg", "rb"), "image/jpeg")
    }
    data = {
        "name": name
    }
    
    # Send the request
    print(f"Adding face to database with name: {name}...")
    response = requests.post(url, files=files, data=data)
    print(f"Response: {json.dumps(response.json(), indent=2)}")
    print("\n" + "="*50 + "\n")

def test_duplicate_face(name: str):
    """Test adding the same face again (should fail)"""
    url = "http://localhost:8000/add_face"
    
    # Prepare the files and form data
    files = {
        "file": (f"{name}.jpeg", open(f"faces/{name}.jpeg", "rb"), "image/jpeg")
    }
    data = {
        "name": f"{name}_duplicate"  # Try to add same face with different name
    }
    
    # Send the request
    print(f"Attempting to add duplicate face with name: {data['name']}...")
    response = requests.post(url, files=files, data=data)
    print(f"Response: {json.dumps(response.json(), indent=2)}")
    print("\n" + "="*50 + "\n")

def test_recognize_face(name: str):
    """Test recognizing a face from the database"""
    url = "http://localhost:8000/recognize_face"
    
    # Prepare the file
    files = {
        "file": (f"{name}.jpeg", open(f"faces/{name}.jpeg", "rb"), "image/jpeg")
    }
    
    # Send the request
    print("Recognizing face...")
    response = requests.post(url, files=files)
    print(f"Response: {json.dumps(response.json(), indent=2)}")
    print("\n" + "="*50 + "\n")

def test_delete_face(name: str):
    """Test deleting a face from the database"""
    url = f"http://localhost:8000/delete_face/{name}"
    
    # Send the request
    print(f"Deleting face with name: {name}...")
    response = requests.delete(url)
    print(f"Response: {json.dumps(response.json(), indent=2)}")
    print("\n" + "="*50 + "\n")

    # Try to recognize the face again (should fail or return no matches)
    test_recognize_face(name)

if __name__ == "__main__":
    import sys
    
    # Get the name from command line arguments or use default
    name = sys.argv[1] if len(sys.argv) > 1 else "derek"
    
    # First add the face to the database
    test_add_face(name)
    
    # Try to add the same face again (should fail)
    # test_duplicate_face(name)
    
    # # Try to recognize the face
    # test_recognize_face(name)
    
    # # Delete the face and verify it's gone
    # test_delete_face(name)
    