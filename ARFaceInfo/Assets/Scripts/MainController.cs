﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;

public class MainController : MonoBehaviour
{

    public Camera renderCamera;
    public GameObject fakeFace;
    public FeedbackTexture feedbackTexture;
    [Range(0.0f, 10000.0f)] public float multiplier;
    [Range(0.0f, 100.0f)] public float baseDist;
    public AzureFaceConnection azureFaceConnection;

    AzureFaceConnection.AzureDetectResult[] lastBoxes;
    
    int numDetectedFaces = 0;
    List<Vector4> faceBoundCoords = new List<Vector4>();

    // Start is called before the first frame update
    void Start()
    {

        // azureFaceConnection = AzureFaceConnection(string endpoint, string key, string pgUuid);

    }

    // Update is called once per frame
    void Update()
    {
        
        if(feedbackTexture.newImageBytesAvailable) {
            feedbackTexture.newImageBytesAvailable = false;

            var imageBytes =  feedbackTexture.imageBytes;
            azureFaceConnection.StartDetectFace(imageBytes);

        }
        // Check for bounding boxes
        AzureFaceConnection.AzureDetectResult[] newBoxes;
        if (azureFaceConnection.TryGetNewBoundingBox(out newBoxes)) {
            // do something with bounding boxes
            numDetectedFaces = newBoxes.Length;


            faceBoundCoords.Clear();
            foreach (var currentBox in newBoxes){
                var currRect = currentBox.faceRectangle;
                faceBoundCoords.Add(new Vector4(currRect.left, currRect.top, currRect.width, currRect.height));
            }
            Vector3 point = new Vector3();
            foreach (var currentFBC in faceBoundCoords)
            {
                Vector2 centerPos = new Vector2();
                centerPos.x = currentFBC.x + currentFBC.z / 2;
                centerPos.y = renderCamera.pixelHeight - (currentFBC.y + currentFBC.w / 2);
                float length = Vector2.Distance(new Vector2(currentFBC.x, currentFBC.y), new Vector2(currentFBC.x+currentFBC.z, currentFBC.y+ currentFBC.w));
                point = renderCamera.ScreenToWorldPoint(new Vector3(centerPos.x, centerPos.y, baseDist + multiplier / length));
            }
            fakeFace.transform.position = point;
            //TrackFaces(lastBoxes, newBoxes);
        }
    }

    void OnGUI() {
        const int labelHeight = 60;
        const int boundary = 20;

        GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 40;
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

        GUI.Label(new Rect(Screen.width - boundary - 200, Screen.height - labelHeight - boundary, 400, labelHeight),
                  $"# faces: {numDetectedFaces}");

        
    }

}
