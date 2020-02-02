using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARExtensions;
using UnityEngine.XR.ARFoundation;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;


public sealed class SeeThroughController : MonoBehaviour {
    #region Editor public fields

    [SerializeField]
    Camera seeThroughCamera;

    [SerializeField]
    Camera renderingCamera;

    [SerializeField]
    Material backgroundMaterial;

#if UNITY_EDITOR
    [SerializeField]
    Material debugBackgroundMaterial;
#endif

    #endregion

    #region Public properties

    public Material BackgroundMaterial {
        get { return backgroundMaterial; }
        set {
            backgroundMaterial = value;

            seeThroughRenderer.Mode = UnityEngine.XR.ARRenderMode.MaterialAsBackground;
            seeThroughRenderer.BackgroundMaterial = backgroundMaterial;

            if (ARSubsystemManager.cameraSubsystem != null) {
                ARSubsystemManager.cameraSubsystem.Material = backgroundMaterial;
            }
        }
    }

    #endregion

    #region Private fields

    SeeThroughRenderer seeThroughRenderer;

#if UNITY_EDITOR
    WebCamTexture webCamTexture;
#endif

    #endregion

    #region Unity methods

    void Start() {

#if UNITY_EDITOR
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();

        debugBackgroundMaterial.SetTexture("_MainTex", webCamTexture);

        seeThroughRenderer = new SeeThroughRenderer(seeThroughCamera, debugBackgroundMaterial);
#else
        seeThroughRenderer = new SeeThroughRenderer(seeThroughCamera, backgroundMaterial);
#endif

        var cameraSubsystem = ARSubsystemManager.cameraSubsystem;
        if (cameraSubsystem != null) {
            cameraSubsystem.Camera = seeThroughCamera;
            cameraSubsystem.Material = BackgroundMaterial;
        }

        ARSubsystemManager.cameraFrameReceived += OnCameraFrameReceived;
    }

    
    // // Update is called once per frame
    // unsafe void Update()
    // {
    //     if (Time.frameCount % frameDelay == 0) {
    //         CameraImage cameraImage;

    //         Texture2D cameraTexture;    //Put this back at the top

    //         // The following WORKS
    //         //ARSubsystemManager.cameraSubsystem.GetTextures(cameraTexture);
    //         var cameraSubsystem = ARSubsystemManager.cameraSubsystem;

    //         cameraSubsystem.TryGetLatestImage(out cameraImage);

    //         // https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/manual/cpu-camera-image.html?q=OnCameraFrameReceived
    //         var conversionParams = new CameraImageConversionParams
    //         {
    //             // Get the entire image.  Downsample by 2.  Greyscale Alpha 8
    //             inputRect = new RectInt(0, 0, cameraImage.width, cameraImage.height),
    //             outputDimensions = new Vector2Int(cameraImage.width / 2, cameraImage.height / 2),
    //             outputFormat = TextureFormat.Alpha8,
    //         };

    //         // See how many bytes we need to store the final image.
    //         int size = cameraImage.GetConvertedDataSize(conversionParams);

    //         // Allocate a buffer to store the image
    //         var buffer = new NativeArray<byte>(size, Allocator.Temp);

    //         // Extract the image data
    //         cameraImage.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

    //         // The image was converted to RGBA32 format and written into the provided buffer
    //         // so we can dispose of the CameraImage. We must do this or it will leak resources.
    //         cameraImage.Dispose();

    //         // Now do something with buffer

    //         cameraTexture = new Texture2D(
    //             conversionParams.outputDimensions.x,
    //             conversionParams.outputDimensions.y,
    //             conversionParams.outputFormat,
    //             false);

    //         cameraTexture.LoadRawTextureData(buffer);
    //         cameraTexture.Apply();

    //         // Done with our temporary data
    //         buffer.Dispose();

    //         byte[] bytes = cameraTexture.EncodeToJPG();

    //         Graphics.Blit(cameraTexture, rt); 
    //     }
    // }

    void OnGUI() {
        const int labelHeight = 60;
        const int sliderWidth = 200;
        const int buttonSize = 80;
        const int boundary = 20;
        const float arFovIncrement = 0.001f;

        GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 40;
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;

        GUI.Label(new Rect(boundary, boundary, 400, labelHeight),
                  $"AR FOV: {renderingCamera.fieldOfView:F3}");

        if (GUI.Button(new Rect(boundary, boundary + labelHeight, buttonSize, buttonSize), "-")) {
            renderingCamera.fieldOfView -= arFovIncrement;
        }

        renderingCamera.fieldOfView = GUI.HorizontalSlider(
            new Rect(boundary + buttonSize, boundary + labelHeight, sliderWidth, labelHeight),
            renderingCamera.fieldOfView, 35f, 42f);

        if (GUI.Button(
            new Rect(boundary + buttonSize + sliderWidth, boundary + labelHeight, buttonSize,
                     buttonSize), "+")) {
            renderingCamera.fieldOfView += arFovIncrement;
        }
    }

    #endregion

    #region Camera handling

    void SetupCameraIfNecessary() {
        seeThroughRenderer.Mode = UnityEngine.XR.ARRenderMode.MaterialAsBackground;

        if (seeThroughRenderer.BackgroundMaterial != BackgroundMaterial) {
            BackgroundMaterial = BackgroundMaterial; 
        }
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs) {
        SetupCameraIfNecessary();
    }

    #endregion
}
