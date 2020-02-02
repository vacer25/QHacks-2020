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

            var x =  feedbackTexture.imageBytes;
            azureFaceConnection.StartDetectFace(x);


        }
        // Check for bounding boxes
        AzureFaceConnection.AzureDetectResult[] adr;
        if (azureFaceConnection.TryGetNewBoundingBox(out adr)) {
            // do something with bounding boxes
            Debug.Log("# of Boxes: " + adr.Length);
        }
    }
}
