using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedbackTexture : MonoBehaviour
{
    public RenderTexture rt;
    [Range(0,100)] public int frameDelay;

    public byte[] imageBytes;

    public bool newImageBytesAvailable;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (frameDelay == 0 || Time.frameCount % frameDelay == 0) {
            Graphics.Blit(source, rt); 
            Texture2D virtualPhoto = new Texture2D(960,540, TextureFormat.RGB24, false);
            virtualPhoto.ReadPixels( new Rect (0,0,960,540), 0, 0);
            imageBytes = virtualPhoto.EncodeToJPG();
            newImageBytesAvailable = true;
        }

        Graphics.Blit(source, destination);
    }
}
