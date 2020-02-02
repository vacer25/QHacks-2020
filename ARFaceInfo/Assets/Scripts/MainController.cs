using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;

public class MainController : MonoBehaviour
{
    
    class Tuple<T1, T2> {
        public T1 a;
        public T2 b;
    }
    class Triple<T1, T2, T3> {
        public T1 a;
        public T2 b;
        public T3 c;
    }

    public int x_offset;
    public int y_offset;
    public Camera renderCamera;
    public GameObject personInfoTextPrefab;
    public FeedbackTexture feedbackTexture;
    [Range(0.0f, 10000.0f)] public float multiplier;
    [Range(0.0f, 100.0f)] public float baseDist;
    public AzureFaceConnection azureFaceConnection;

    List<string> names = new List<string>();

    public class PersistentBox {
        public int x, y;
        public int w, h;
        public string lastId;
        public int dc;
    }
    const int MaxDestroyCount = 40;
    List<PersistentBox> persistentBoxes = new List<PersistentBox>();
    
    int numPB = 0;
    int numPBActive = 0;
    List<Vector4> faceBoundCoords = new List<Vector4>();
    List<GameObject> personInfoTexts = new List<GameObject>();

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
        
        numPBActive = 0;
        numPB = persistentBoxes.Count;
        for(int j=0;j<persistentBoxes.Count;j+=1){
            if (persistentBoxes[j].dc >= 0) {
                numPBActive += 1;
                persistentBoxes[j].dc--;
                // This is pending for destruction. Don't update centroid
                if(persistentBoxes[j].dc < 0) {
                    azureFaceConnection.ClearFace(j);
                }
            }
        }
        // Check for bounding boxes
        AzureFaceConnection.AzureDetectResult[] newBoxes;
        if (azureFaceConnection.TryGetNewBoundingBox(out newBoxes)) {
            // do something with bounding boxes
            // numDetectedFaces = newBoxes.Length;
            TrackFaces(newBoxes);



            foreach(GameObject currentOldPersonInfoText in personInfoTexts) {
                Destroy(currentOldPersonInfoText);
            }

            names.Clear();
            faceBoundCoords.Clear();
            for(int j=0;j<persistentBoxes.Count;j+=1){
                var box = persistentBoxes[j];
                if (box.dc < 0) continue;
            //  (var currentBox in newBoxes){
                //names.Add(currentBox.faceId);
                azureFaceConnection.StartRecognizeFace(j, box.lastId);
                string name;
                azureFaceConnection.TryGetFaceData(j, out name);
                names.Add(name ?? "<Unknown>");
                // var currRect = currentBox.faceRectangle;
                faceBoundCoords.Add(new Vector4(box.x - box.w/2, box.y - box.h/2, box.w, box.h));
            }
            int i = 0;
            foreach (var currentFBC in faceBoundCoords)
            {
                // Calculate
                Vector3 point = new Vector3();
                Vector2 centerPos = new Vector2();
                centerPos.x = currentFBC.x + currentFBC.z / 2 + x_offset;
                centerPos.y = renderCamera.pixelHeight - (currentFBC.y + currentFBC.w / 2) + y_offset;

                float length = currentFBC.z;
                point = renderCamera.ScreenToWorldPoint(new Vector3(centerPos.x + (centerPos.x - Screen.width/2)*1f, centerPos.y + (centerPos.y - Screen.height/2)*1f, multiplier*1.73205f/(Mathf.Pow(length/960f, 0.5f))));


                GameObject personInfoTextObject = Instantiate(personInfoTextPrefab, point, Quaternion.identity);
                PersonInfoText personInfoText = personInfoTextObject.GetComponent(typeof(PersonInfoText)) as PersonInfoText;
                personInfoText.setText(names[i]);
                personInfoTexts.Add(personInfoTextObject);

                i++;

            }
        }
    }
    void TrackFaces(AzureFaceConnection.AzureDetectResult[] newBoxes) {
        int maxi = persistentBoxes.Count;
        int maxj = newBoxes.Length;
        
        Tuple<int,int>[] centroids = new Tuple<int,int>[newBoxes.Length];
        for(int i=0; i < newBoxes.Length; i+= 1){
            var fr = newBoxes[i].faceRectangle;
            centroids[i] = new Tuple<int,int>(){a=fr.left + fr.width/2,b=fr.top + fr.height/2};
        }
        // float[,] distances = new float[persistentBoxes.Length,newBoxes.Length];
        List<Triple<double, int,int>> alldists = new List<Triple<double,int,int>>(); 
        for (int i = 0; i < maxi; i += 1) {
            if (persistentBoxes[i].dc < 0) continue;
            for (int j = 0, n = 0; j < maxj; j += 1) {
                int dx = persistentBoxes[i].x - centroids[j].a;
                int dy = persistentBoxes[i].y - centroids[j].b;
                // distances[i,j] = Math.sqrt(dx*dx + dy*dy);
                double distance = Math.Sqrt(dx*dx + dy*dy);
                alldists.Add(new Triple<double,int,int>(){a=distance,b=i,c=j});
            }
        }
        alldists.Sort((x,y)=>x.a.CompareTo(y.a));
        bool[] usedold = new bool[maxi];
        bool[] usednew = new bool[maxj];
        int[] map = new int[maxi];

        foreach (var t in alldists) {
            int i = t.b;
            int j = t.c;
            if (persistentBoxes[i].dc < 0 || usedold[i] || usednew[j]) {
                continue;
            }
            usedold[i]=true;
            usednew[j]=true;
            map[i]=j;
        }

        int firstFreeI=maxi;
        for(int i =0;i<maxi;i+=1){
            if(persistentBoxes[i].dc < 0) {
                continue;
            }
            if(!usedold[i]){
                // This is pending for destruction. Don't update centroid
                if(persistentBoxes[i].dc < 0) {
                    azureFaceConnection.ClearFace(i);
                }
            } else {
                persistentBoxes[i].dc = MaxDestroyCount;
                persistentBoxes[i].x = centroids[map[i]].a;
                persistentBoxes[i].y = centroids[map[i]].b;
                persistentBoxes[i].w = newBoxes[map[i]].faceRectangle.width;
                persistentBoxes[i].h = newBoxes[map[i]].faceRectangle.height;
                persistentBoxes[i].lastId = newBoxes[map[i]].faceId;
            }
        }
        for(int j=0;j<maxj;j++){
            if(!usednew[j]){
                // A new face in town.
                int i=0;
                for(;i<maxi;i+=1){
                    if(persistentBoxes[i].dc <0)
                        break;
                }
                if(i>=maxi) {
                    i = persistentBoxes.Count;
                    persistentBoxes.Add(new PersistentBox());
                }
                persistentBoxes[i].dc=MaxDestroyCount;
                persistentBoxes[i].x = centroids[j].a;
                persistentBoxes[i].y = centroids[j].b;
                persistentBoxes[i].w = newBoxes[j].faceRectangle.width;
                persistentBoxes[i].h = newBoxes[j].faceRectangle.height;
                persistentBoxes[i].lastId = newBoxes[j].faceId;
            }
        }
    }

    void OnGUI() {
        const int labelHeight = 60;
        const int boundary = 20;

        GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 40;
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

        GUI.Label(new Rect(Screen.width - boundary - 200, Screen.height - labelHeight - boundary, 400, labelHeight),
                  $"#s: {numPB} {numPBActive}");

        
    }

}
