using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Collections;
using Unity.Collections;
using UnityEngine.Networking;
using System.Text;

public class AzureFaceConnection : MonoBehaviour {
    [Serializable]
    public struct AzureDetectBoundingBox {
        public int left;
        public int top;
        public int width;
        public int height;
    }
    [Serializable]
    public struct AzureDetectResult {
        string faceId;
        public AzureDetectBoundingBox faceRectangle;
    }
    [Serializable]
    public struct AzureDetectResultArray {
        public AzureDetectResult[] array;
    }

    bool _newBoxesAvailable = false;
    int _lastSequenceSent = 0;
    int _lastSequenceReceived = 0;
    AzureDetectResult[] _lastBoxes;

    Dictionary<object, string> _faceDataCache;
    HashSet<object> _outstandingData;

    public string endpoint, key, pgUuid;

    ////////////////////////////////////////////////////////////////////////////
    // Interface
    ////////////////////////////////////////////////////////////////////////////

    // Call this for every JPEG you generate.
    public void StartDetectFace(byte[] jpg) {
        int mySequence = ++_lastSequenceSent;
        StartCoroutine(AzureDetect(jpg, mySequence));
    }
    // Call this every time you are trying to draw bounding boxes (to see if they have moved).
    public bool TryGetNewBoundingBox(out AzureDetectResult[] boxes) {
        bool updated = _newBoxesAvailable;
        _newBoxesAvailable = false;
        boxes = _lastBoxes;
        return updated;
    }

    // Call this every time you find a face.
    public void StartRecognizeFace(object token, string faceId) {
        if (_outstandingData.Contains(token) || _faceDataCache.ContainsKey(token)) {
            return;
        }
        _outstandingData.Add(token);
        StartCoroutine(AzureIdentify(token, faceId));
    }
    // Returns false if data is not ready, and true if data is ready.
    public bool TryGetFaceData(object token, out string data) {
        data = null;
        if (_faceDataCache.ContainsKey(token)) {
            data = _faceDataCache[token];
            return true;
        }
        return false;
    }
    public void ClearFace(object token) {
        _faceDataCache.Remove(token);
    }

    ////////////////////////////////////////////////////////////////////////////
    // Implementation
    ////////////////////////////////////////////////////////////////////////////
    // Private method to query Azure to detect faces in a JPEG.
    private IEnumerator AzureDetect(byte[] jpg, int mySequence) {
        UnityWebRequest wr = new UnityWebRequest();
        wr.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
        wr.url = new StringBuilder()
            .Append(endpoint)
            .Append("/detect")
            .ToString();
        wr.method = UnityWebRequest.kHttpVerbPOST;
        wr.SetRequestHeader("Content-Type", "application/octet-stream");

        wr.uploadHandler = new UploadHandlerRaw(jpg);
        wr.downloadHandler = new DownloadHandlerBuffer();

        yield return wr.SendWebRequest();
        if (wr.isNetworkError || wr.isHttpError) {
            Debug.Log(wr.error);
        } else {
            if (mySequence > _lastSequenceReceived) {
                _lastBoxes = JsonUtility.FromJson<AzureDetectResultArray>("{\"array\":"+wr.downloadHandler.text+"}").array;
                _lastSequenceReceived = mySequence;
                _newBoxesAvailable = true;
            }
        }
    }

    // Private method to query Azure to identify the faces we just had.
    private IEnumerator AzureIdentify(object token, string faceId) {
        yield break;
    }
}
