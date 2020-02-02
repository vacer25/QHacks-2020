using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;

public class MainController : MonoBehaviour
{

    //GameObject renderCamera;

    public FeedbackTexture feedbackTexture;
    
    public AzureFaceConnection azureFaceConnection;

    AzureFaceConnection.AzureDetectResult[] lastBoxes;
    
    int numDetectedFaces = 0;

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
