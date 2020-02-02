using UnityEngine;
using System;

public class LookAtTarget : MonoBehaviour
{

    void Start() {
        // transform.LookAt(Camera.main.transform);
        // transform.Rotate(0, 180, 0);

        Vector3 targetPostition = Camera.main.transform.position;

        transform.LookAt(targetPostition);
        transform.Rotate(0, 180, 0);

    }

    // Update is called once per frame
    void Update()
    {

        // Vector3 targetPostition = new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z) ;

        Vector3 targetPostition = Camera.main.transform.position;

        transform.LookAt(targetPostition);
        transform.Rotate(0, 180, 0);
    }
}
