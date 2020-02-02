using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PersonInfoText : MonoBehaviour
{

    public TextMeshPro textMesh;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setText(string textToSet) {
        textMesh.text = textToSet;
    }

}
