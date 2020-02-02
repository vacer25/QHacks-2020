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
        public string faceId;
        public AzureDetectBoundingBox faceRectangle;
    }
    [Serializable]
    public struct AzureDetectResultArray {
        public AzureDetectResult[] array;
    }

    [Serializable]
    public struct AzureIdentifyPrompt {
        public string personGroupId;
        public string[] faceIds;
    }

    [Serializable]
    public struct AzureIdentifyCandidate {
        public string personId;
        public double confidence;
    }
    [Serializable]
    public struct AzureIdentifyResult {
        public string faceId;
        public AzureIdentifyCandidate[] candidates;
    }
    [Serializable]
    public struct AzureIdentifyResultArray {
        public AzureIdentifyResult[] array;
    }

    [Serializable]
    public struct AzurePersonResult {
        public string personId;
        public string[] persistedFaceIds;
        public string name;
        public string userData;
    }


    bool _newBoxesAvailable = false;
    int _lastSequenceSent = 0;
    int _lastSequenceReceived = 0;
    AzureDetectResult[] _lastBoxes;

    Dictionary<object, string> _faceDataCache = new Dictionary<object, string>();
    HashSet<object> _outstandingData = new HashSet<object>();

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
        if (_outstandingData.Contains(token))
            _outstandingData.Remove(token);
        if (_faceDataCache.ContainsKey(token))
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
            .Append("?recognitionModel=recognition_02")
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
        string personId;
        {
            UnityWebRequest wr = new UnityWebRequest();
            wr.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            wr.url = new StringBuilder()
                .Append(endpoint)
                .Append("/identify")
                .ToString();
            wr.method = UnityWebRequest.kHttpVerbPOST;
            wr.SetRequestHeader("Content-Type", "application/json");

            AzureIdentifyPrompt prompt = new AzureIdentifyPrompt(){
                personGroupId = pgUuid,
                faceIds = new string[]{faceId}
            };

            string prompt_text = JsonUtility.ToJson(prompt);

            wr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(prompt_text));
            wr.downloadHandler = new DownloadHandlerBuffer();

            yield return wr.SendWebRequest();
            if (wr.isNetworkError || wr.isHttpError) {
                Debug.Log("identify " + wr.error + " " + wr.downloadHandler.text);
                _outstandingData.Remove(token);
                yield break;
            }
            var identify_results = JsonUtility.FromJson<AzureIdentifyResultArray>("{\"array\":"+wr.downloadHandler.text+"}").array;
            if (identify_results.Length != 1) {
                Debug.Log("[token = "+token.ToString()+"] Invalid number of results");
                _outstandingData.Remove(token);
                yield break;
            }
            var ident = identify_results[0];
            if (ident.candidates.Length != 1) {
                Debug.Log("[token = "+token.ToString()+"] Invalid number of candidates");
                _outstandingData.Remove(token);
                yield break;
            }
            personId = ident.candidates[0].personId;
        }
        {
            UnityWebRequest wr = new UnityWebRequest();
            wr.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            wr.url = new StringBuilder()
                .Append(endpoint)
                .Append("/persongroups/")
                .Append(pgUuid)
                .Append("/persons/")
                .Append(personId)
                .ToString();
            wr.method = UnityWebRequest.kHttpVerbGET;
            // wr.SetRequestHeader("Content-Type", "application/json");

            // string prompt_text = JsonUtility.ToJson<AzureIdentifyPrompt>(prompt);
            // wr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(prompt_text)));
            wr.downloadHandler = new DownloadHandlerBuffer();

            yield return wr.SendWebRequest();
            if (wr.isNetworkError || wr.isHttpError) {
                Debug.Log("person get" + wr.error);
                _outstandingData.Remove(token);
                yield break;
            }
            var result = JsonUtility.FromJson<AzurePersonResult>(wr.downloadHandler.text);
            _faceDataCache[token] = result.name;
            _outstandingData.Remove(token);
        }
    }
}
