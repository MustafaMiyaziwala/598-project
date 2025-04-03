using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

/// <summary>
/// A more reliable HTTP client for Unity that handles network requests with better error handling
/// </summary>
public class HttpClient : MonoBehaviour
{
    private static HttpClient _instance;
    public static HttpClient Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("HttpClient");
                _instance = go.AddComponent<HttpClient>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Makes a GET request to the specified URL and returns the result
    /// </summary>
    /// <param name="url">The URL to request</param>
    /// <param name="callback">Callback function to handle the response</param>
    /// <param name="timeout">Timeout in seconds (default: 10)</param>
    public void Get(string url, Action<string> callback, int timeout = 10)
    {
        StartCoroutine(GetCoroutine(url, callback, timeout));
    }

    /// <summary>
    /// Makes a GET request to the specified URL and returns the result as a Task
    /// </summary>
    /// <param name="url">The URL to request</param>
    /// <param name="timeout">Timeout in seconds (default: 10)</param>
    /// <returns>A Task that completes with the response string</returns>
    public async Task<string> GetAsync(string url, int timeout = 10)
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        
        Get(url, (result) => tcs.SetResult(result), timeout);
        
        return await tcs.Task;
    }

    private IEnumerator GetCoroutine(string url, Action<string> callback, int timeout)
    {
        Debug.Log($"Starting GET request to: {url}");
        callback?.Invoke("Pre-request");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = timeout;
            
            // Set up request headers if needed
            // request.SetRequestHeader("Content-Type", "application/json");
            
            Debug.Log("Sending web request...");
            
            // Send the request
            yield return request.SendWebRequest();
            
            Debug.Log("Request completed");

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Request successful. Response: {responseText}");
                callback?.Invoke(responseText);
            }
            else
            {
                string errorMessage = $"Request failed: {request.error} (Result: {request.result}, Code: {request.responseCode})";
                Debug.LogError(errorMessage);
                callback?.Invoke($"Error: {errorMessage}");
            }
        }
    }

    /// <summary>
    /// Makes a POST request to the specified URL with the given data
    /// </summary>
    /// <param name="url">The URL to request</param>
    /// <param name="jsonData">JSON data to send</param>
    /// <param name="callback">Callback function to handle the response</param>
    /// <param name="timeout">Timeout in seconds (default: 10)</param>
    public void Post(string url, string jsonData, Action<string> callback, int timeout = 10)
    {
        StartCoroutine(PostCoroutine(url, jsonData, callback, timeout));
    }

    /// <summary>
    /// Makes a POST request to the specified URL with the given data as a Task
    /// </summary>
    /// <param name="url">The URL to request</param>
    /// <param name="jsonData">JSON data to send</param>
    /// <param name="timeout">Timeout in seconds (default: 10)</param>
    /// <returns>A Task that completes with the response string</returns>
    public async Task<string> PostAsync(string url, string jsonData, int timeout = 10)
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        
        Post(url, jsonData, (result) => tcs.SetResult(result), timeout);
        
        return await tcs.Task;
    }

    private IEnumerator PostCoroutine(string url, string jsonData, Action<string> callback, int timeout)
    {
        Debug.Log($"Starting POST request to: {url}");
        callback?.Invoke("Pre-request");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeout;
            
            // Set up request headers
            request.SetRequestHeader("Content-Type", "application/json");
            
            Debug.Log("Sending web request...");
            
            // Send the request
            yield return request.SendWebRequest();
            
            Debug.Log("Request completed");

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"Request successful. Response: {responseText}");
                callback?.Invoke(responseText);
            }
            else
            {
                string errorMessage = $"Request failed: {request.error} (Result: {request.result}, Code: {request.responseCode})";
                Debug.LogError(errorMessage);
                callback?.Invoke($"Error: {errorMessage}");
            }
        }
    }
} 