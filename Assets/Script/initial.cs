using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ModelLoader : MonoBehaviour
{
    string filePath;
    private Pose PlacementPose;
    private ARRaycastManager aRRaycastManager;
    public GameObject placementIndicator;
    private GameObject model;
    public GameObject avatar;
    private bool placementPoseIsValid = false;
    GameObject instantiatedModel;
    private Pose initialPlacementPose; 

    // Rotation sensitivity
    public float rotateSensitivity = 1.0f;

    private void Start()
    {
        Debug.Log("UNITY::Tap started");
        aRRaycastManager = FindObjectOfType<ARRaycastManager>();
        filePath = $"{Application.persistentDataPath}/Files/";
        // Application.quitting += OnApplicationQuit;
    }
     void Update()
    {
        if(instantiatedModel==null  && placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Debug.Log("UNITY::Tap updated");
            armodel();
        }
        UpdatePlacementPose();
        UpdatePlacementIndicator();


        // For 360 rotation
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            // Get the touch delta position
            Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;

            // Calculate the rotation angle based on the touch delta position
            float rotationAngle = touchDeltaPosition.x * rotateSensitivity;

            // Rotate the model around the y-axis
            instantiatedModel.transform.Rotate(0, rotationAngle, 0, Space.World);
        }


        // For zoom in and zoom out
        if (Input.touchCount == 2)
        {
            // Get the touches
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            Vector3 localScale = instantiatedModel.transform.localScale;

            // // Check the sign of deltaMagnitudeDiff
            // if (deltaMagnitudeDiff > 0)
            // {
            //     // the distance between the touches has increased, so the user is zooming out
            //     instantiatedModel.transform.localScale -= new Vector3(deltaMagnitudeDiff * 0.01f, deltaMagnitudeDiff * 0.01f, deltaMagnitudeDiff * 0.01f);
            // }
            // else
            // {
            //     // the distance between the touches has decreased, so the user is zooming in
            //     instantiatedModel.transform.localScale += new Vector3(-deltaMagnitudeDiff * 0.01f,-deltaMagnitudeDiff * 0.01f,-deltaMagnitudeDiff * 0.01f);
            // }
            if (deltaMagnitudeDiff > 0)
            {
                // the distance between the touches has increased, so the user is zooming out
                localScale -= new Vector3(deltaMagnitudeDiff * 0.01f, deltaMagnitudeDiff * 0.01f, deltaMagnitudeDiff * 0.01f);
            }
            else
            {
                // negate deltaMagnitudeDiff before adding it to the scale 
                localScale += new Vector3(-deltaMagnitudeDiff * 0.01f, -deltaMagnitudeDiff * 0.01f, -deltaMagnitudeDiff * 0.01f);
            }
            // Check the minimum limit of the scale
            localScale.x = Mathf.Max(localScale.x, 0.1f);
            localScale.y = Mathf.Max(localScale.y, 0.1f);
            localScale.z = Mathf.Max(localScale.z, 0.1f);
            instantiatedModel.transform.localScale = localScale;
    
        }
    }

    void armodel(){
        if(model == null){
            Debug.Log("UNITY::AR Model Function");
            initialPlacementPose = new Pose(PlacementPose.position, PlacementPose.rotation);
            // instantiatedModel=GameObject.Instantiate(avatar, PlacementPose.position, PlacementPose.rotation*Quaternion.Euler(0, 180, 0));
            Vector3 offset = new Vector3(0.2f, 0, 0.1f);
            instantiatedModel=GameObject.Instantiate(avatar, initialPlacementPose.position+offset, initialPlacementPose.rotation*Quaternion.Euler(90, 0, 135));
        }
         else
        {
            if (instantiatedModel != null)
            {
                instantiatedModel.SetActive(false);
            }
            instantiatedModel = GameObject.Instantiate(model, initialPlacementPose.position, model.transform.rotation);
            instantiatedModel.SetActive(true);
            model.SetActive(false);

        }
    }

    [Serializable]
    private class Data
    {
        public string input;
    }

    public void changeBurger(string url)
    {
        // ResetWrapper();
        var urlData = JsonUtility.FromJson<Data>(url);
        Debug.Log("burger button pressed");
        Debug.Log("burger url: " + urlData.input);
        DownloadFile(urlData.input);
    }

    public void DownloadFile(string url)
    {
        if(model != null){
            Destroy(model);
            model =null;
        }
        string path = GetFilePath(url);
        if (File.Exists(path))
        {
            Debug.Log("Found file locally, loading...");
            LoadModel(path);
            armodel();
            return;
        }
        StartCoroutine(GetFileRequest(url, (UnityWebRequest req) =>
        {
            if (req.isNetworkError || req.isHttpError)
            {
                Debug.Log($"{req.error} : {req.downloadHandler.text}");
            } else
            {
                LoadModel(path);
                armodel();
                return;
            }
        }));
    }

    string GetFilePath(string url)
    {
        string[] pieces = url.Split('/');
        string filename = pieces[pieces.Length - 1];
        return $"{filePath}{filename}";
    }

    void LoadModel(string path)
    {
        // Debug.Log("1");
        // Debug.Log(path);
        // Debug.Log("2");
        // Debug.Log(filePath);
        model = Importer.LoadFromFile(path);
    }

    IEnumerator GetFileRequest(string url, Action<UnityWebRequest> callback)
    {
        using(UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerFile(GetFilePath(url));
            yield return req.SendWebRequest();
            callback(req);
        }
    }

     void UpdatePlacementIndicator()
    {
        // Debug.Log("UNITY::into placement indicator function");
        
        if(instantiatedModel==null  && placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(PlacementPose.position, PlacementPose.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);

        }
    }

    void UpdatePlacementPose()
    {
        // Debug.Log("UNITY::UpdatePlacementPose function");
        
        var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        aRRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if(placementPoseIsValid)
        {
            PlacementPose = hits[0].pose;
        }
    }
   public void reset(){
        Debug.Log("UNity::Reset Function");
        Destroy(model);
        model=null;
        Destroy(instantiatedModel);
        instantiatedModel = null;
        placementPoseIsValid=false;
    }
    // void OnApplicationQuit()
    // {
    //     // Delete all downloaded 3D model files
    //     // Debug.Log("Delete/////////////////////////////////////////////////////////////////////////////////////////////////");
    //     Directory.Delete(filePath, true);
    // }

}

