using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

    public class PutMessenger : MonoBehaviour
{
    public TextMeshPro textBox;

    readonly string getURL = "https://us-central1-affable-armor-266916.cloudfunctions.net/hello-world-function";
    void Start()
    {
         StartCoroutine(SimpleGetRequest());
    }

    IEnumerator SimpleGetRequest()
    {
        byte[] myData = System.Text.Encoding.UTF8.GetBytes("This is some test data");

        UnityWebRequest www = UnityWebRequest.Put(getURL, myData);
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            string result = www.downloadHandler.text;
            textBox.text = result;
        }
    }
}