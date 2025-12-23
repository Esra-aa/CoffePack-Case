using System.Collections;
using System.IO;
using UnityEngine;

public class ObjectScreenshotCapture : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode captureKey = KeyCode.S;
    public string fileName = "ObjectScreenshot.png";
    public int captureWidth = 1920;
    public int captureHeight = 1080;

    [Header("References")]
    public Camera targetCamera;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            StartCoroutine(CaptureTransparentScreenshot());
        }
    }

    private IEnumerator CaptureTransparentScreenshot()
    {
        yield return new WaitForEndOfFrame();

        if (targetCamera == null)
        {
            Debug.LogError("Target Camera is not assigned!");
            yield break;
        }

        // Remember current camera settings
        CameraClearFlags originalClearFlags = targetCamera.clearFlags;
        Color originalBackgroundColor = targetCamera.backgroundColor;
        RenderTexture originalTargetTexture = targetCamera.targetTexture;

        // URP Specific: Handle Post Processing
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData urpCamData = 
            targetCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        
        bool originalPostProcessing = false;
        if (urpCamData != null)
        {
            originalPostProcessing = urpCamData.renderPostProcessing;
            urpCamData.renderPostProcessing = false; // Disable PP for transparency
            // Also optional: disable AA if it causes issues
            // urpCamData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None; 
        }

        // Configure camera for transparency
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent black

        // Create a temporary RenderTexture with ARGB32 for transparency support
        // Note: In URP, we might need to ensure the format is high enough precision or standard
        RenderTexture rt = RenderTexture.GetTemporary(captureWidth, captureHeight, 32, RenderTextureFormat.ARGB32);
        
        // Clear the RT explicitly to be safe
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previousActive;

        targetCamera.targetTexture = rt;

        // Render the camera view to the RenderTexture
        targetCamera.Render();

        // Read the RenderTexture into a Texture2D
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
        screenShot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        screenShot.Apply();

        // Restore camera settings
        targetCamera.targetTexture = originalTargetTexture;
        targetCamera.backgroundColor = originalBackgroundColor;
        targetCamera.clearFlags = originalClearFlags;
        
        if (urpCamData != null)
        {
            urpCamData.renderPostProcessing = originalPostProcessing;
        }

        RenderTexture.active = null; // Reset active RT
        RenderTexture.ReleaseTemporary(rt);

        // Encode and save
        byte[] bytes = screenShot.EncodeToPNG();
        Destroy(screenShot);

        // Save to Project Root (beside Assets folder) for easy access
        string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, fileName);
        
        // Ensure unique filename if it exists
        int count = 1;
        while (File.Exists(filePath))
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, $"{nameWithoutExt}_{count}{ext}");
            count++;
        }

        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"<color=green>Screenshot saved to: <b>{filePath}</b></color>");
    }
}
