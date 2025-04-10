using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FaceAnalysis : MonoBehaviour
{
    /// <summary>
    /// Allows this class to behave like a singleton
    /// </summary>
    public static FaceAnalysis Instance;

    /// <summary>
    /// The analysis result text
    /// </summary>
    private TextMesh labelText;

    /// <summary>
    /// Bytes of the image captured with camera
    /// </summary>
    internal byte[] imageBytes;

    /// <summary>
    /// Path of the image captured with camera
    /// </summary>
    internal string imagePath;

    /// <summary>
    /// Base endpoint of Face Recognition Service
    /// </summary>
    const string baseEndpoint = "https://westus.api.cognitive.microsoft.com/face/v1.0/";

    /// <summary>
    /// Auth key of Face Recognition Service
    /// </summary>
    private const string key = "8rtCNEEq0LPfOf9VFC5bBNVolUDQ8XB65HsJn6UC4aDE1DaB7z3oJQQJ99BDACYeBjFXJ3w3AAAKACOG2gUA";

    /// <summary>
    /// Id (name) of the created person group 
    /// </summary>
    private const string personGroupId = "testgroup";


    private string curLabel = "Initial";


    [System.Serializable]
    public class ApiResponse
    {
        public string name;
    }


    /// <summary>
    /// Initialises this class
    /// </summary>
    private void Awake()
    {
        // Allows this instance to behave like a singleton
        Instance = this;

        // Add the ImageCapture Class to this Game Object
        //gameObject.AddComponent<ImageCapture>();

        // Create the text label in the scene
    }


    private void Update()
    {
        //CreateLabel();
    }

    private void Start()
    {
        // Use the new HttpClient to make the request
        HttpClient.Instance.Get("http://35.2.164.171:8000/", CreateLabel);
    }

    /// <summary>
    /// Spawns cursor for the Main Camera
    /// </summary>
    private void CreateLabel(string label)
    {
        // Check if labelText already exists to prevent duplicate text objects
        if (labelText != null)
            return;

        GameObject newLabel = new GameObject("FaceAnalysisLabel");

        newLabel.transform.parent = gameObject.transform;
        newLabel.transform.localPosition = new Vector3(0.2f, 1.6f, 0.5f);
        newLabel.transform.localScale = Vector3.one * 0.02f; // Reduce scale for clarity

        labelText = newLabel.AddComponent<TextMesh>();
        labelText.fontSize = 50;
        labelText.color = Color.white; // Ensure it's readable
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.text = label;
    }


    /// <summary>
    /// Makes a GET request to the given URL and returns the result via callback.
    /// </summary>
    public IEnumerator GetRequest(string url, System.Action<string> callback)
    {
        // This method is kept for backward compatibility
        // It now uses the HttpClient to make the request
        HttpClient.Instance.Get(url, callback);
        yield return null; // Return immediately since the HttpClient handles the coroutine
    }




    /// <summary>
    /// Detect faces from a submitted image
    /// </summary>
    internal IEnumerator DetectFacesFromImage()
    {
        string detectFacesEndpoint = $"{baseEndpoint}detect";

        // Change the image into a bytes array
        imageBytes = GetImageAsByteArray(imagePath);
        
        // Create a custom request with the image data
        using (UnityWebRequest www = new UnityWebRequest(detectFacesEndpoint, "POST"))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.SetRequestHeader("Content-Type", "application/octet-stream");
            www.uploadHandler = new UploadHandlerRaw(imageBytes);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Face_RootObject[] face_RootObject =
                    JsonConvert.DeserializeObject<Face_RootObject[]>(jsonResponse);

                List<string> facesIdList = new List<string>();
                // Create a list with the face Ids of faces detected in image
                foreach (Face_RootObject faceRO in face_RootObject)
                {
                    facesIdList.Add(faceRO.faceId);
                    Debug.Log($"Detected face - Id: {faceRO.faceId}");
                }

                StartCoroutine(IdentifyFaces(facesIdList));
            }
            else
            {
                Debug.LogError($"Face detection failed: {www.error}");
            }
        }
    }

    /// <summary>
    /// Returns the contents of the specified file as a byte array.
    /// </summary>
    static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }

    /// <summary>
    /// Identify the faces found in the image within the person group
    /// </summary>
    internal IEnumerator IdentifyFaces(List<string> listOfFacesIdToIdentify)
    {
        // Create the object hosting the faces to identify
        FacesToIdentify_RootObject facesToIdentify = new FacesToIdentify_RootObject();
        facesToIdentify.faceIds = new List<string>();
        facesToIdentify.personGroupId = personGroupId;
        foreach (string facesId in listOfFacesIdToIdentify)
        {
            facesToIdentify.faceIds.Add(facesId);
        }
        facesToIdentify.maxNumOfCandidatesReturned = 1;
        facesToIdentify.confidenceThreshold = 0.5;

        // Serialize to Json format
        string facesToIdentifyJson = JsonConvert.SerializeObject(facesToIdentify);
        
        string identifyEndpoint = $"{baseEndpoint}identify";
        
        using (UnityWebRequest www = new UnityWebRequest(identifyEndpoint, "POST"))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.SetRequestHeader("Content-Type", "application/json");
            byte[] facesData = Encoding.UTF8.GetBytes(facesToIdentifyJson);
            www.uploadHandler = new UploadHandlerRaw(facesData);
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"Get Person - jsonResponse: {jsonResponse}");
                Candidate_RootObject[] candidate_RootObject = JsonConvert.DeserializeObject<Candidate_RootObject[]>(jsonResponse);

                // For each face to identify that has been submitted, display its candidate
                foreach (Candidate_RootObject candidateRO in candidate_RootObject)
                {
                    if (candidateRO.candidates != null && candidateRO.candidates.Count > 0)
                    {
                        StartCoroutine(GetPerson(candidateRO.candidates[0].personId));
                    }

                    // Delay the next "GetPerson" call, so all faces candidate are displayed properly
                    yield return new WaitForSeconds(3);
                }
            }
            else
            {
                Debug.LogError($"Face identification failed: {www.error}");
            }
        }
    }

    /// <summary>
    /// Provided a personId, retrieve the person name associated with it
    /// </summary>
    internal IEnumerator GetPerson(string personId)
    {
        string getGroupEndpoint = $"{baseEndpoint}persongroups/{personGroupId}/persons/{personId}?";
        
        using (UnityWebRequest www = UnityWebRequest.Get(getGroupEndpoint))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.downloadHandler = new DownloadHandlerBuffer();
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;

                Debug.Log($"Get Person - jsonResponse: {jsonResponse}");
                IdentifiedPerson_RootObject identifiedPerson_RootObject = JsonConvert.DeserializeObject<IdentifiedPerson_RootObject>(jsonResponse);

                // Display the name of the person in the UI
                if (labelText != null)
                {
                    labelText.text = identifiedPerson_RootObject.name;
                }
            }
            else
            {
                Debug.LogError($"Get person failed: {www.error}");
            }
        }
    }



}

/// <summary>
/// The Person Group object
/// </summary>
public class Group_RootObject
{
    public string personGroupId { get; set; }
    public string name { get; set; }
    public object userData { get; set; }
}

/// <summary>
/// The Person Face object
/// </summary>
public class Face_RootObject
{
    public string faceId { get; set; }
}

/// <summary>
/// Collection of faces that needs to be identified
/// </summary>
public class FacesToIdentify_RootObject
{
    public string personGroupId { get; set; }
    public List<string> faceIds { get; set; }
    public int maxNumOfCandidatesReturned { get; set; }
    public double confidenceThreshold { get; set; }
}

/// <summary>
/// Collection of Candidates for the face
/// </summary>
public class Candidate_RootObject
{
    public string faceId { get; set; }
    public List<Candidate> candidates { get; set; }
}

public class Candidate
{
    public string personId { get; set; }
    public double confidence { get; set; }
}

/// <summary>
/// Name and Id of the identified Person
/// </summary>
public class IdentifiedPerson_RootObject
{
    public string personId { get; set; }
    public string name { get; set; }
}