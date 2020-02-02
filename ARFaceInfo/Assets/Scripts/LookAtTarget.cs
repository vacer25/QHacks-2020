using UnityEngine;
using System;

public class LookAtTarget : MonoBehaviour
{
    public Transform target;

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            transform.LookAt(target);
            transform.Rotate(0, 180, 0);
        }
    }
}