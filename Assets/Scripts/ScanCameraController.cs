using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScanCameraController : MonoBehaviour
{
    private static ScanCameraController instance;

    [Header("Scan Preview")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private string targetSceneName = "ScanPage";

    [Header("Camera Settings")]
    [SerializeField] private bool preferRearCamera = true;
    [SerializeField] private Vector2Int preferredResolution = new Vector2Int(1280, 720);
    [SerializeField] private int requestedFps = 30;

    private WebCamTexture webcamTexture;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!ShouldRunInCurrentScene())
        {
            return;
        }

        if (previewImage == null)
        {
            previewImage = GetComponent<RawImage>();
        }

        InitializeRearCamera();
    }

    private bool ShouldRunInCurrentScene()
    {
        return string.IsNullOrEmpty(targetSceneName) ||
               SceneManager.GetActiveScene().name.Equals(targetSceneName, StringComparison.OrdinalIgnoreCase);
    }

    public void InitializeRearCamera()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("Nenhuma câmera disponível para o scan.");
            return;
        }

        int selectedIndex = -1;

        for (int i = 0; i < WebCamTexture.devices.Length; i++)
        {
            var device = WebCamTexture.devices[i];
            if (device.isFrontFacing == preferRearCamera)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        var selectedDevice = WebCamTexture.devices[selectedIndex];
        webcamTexture = new WebCamTexture(selectedDevice.name, preferredResolution.x, preferredResolution.y, requestedFps);

        if (previewImage != null)
        {
            previewImage.texture = webcamTexture;
        }

        webcamTexture.Play();

        if (!webcamTexture.isPlaying)
        {
            Debug.LogWarning($"Não foi possível iniciar a câmera traseira: {selectedDevice.name}");
        }
    }

    private void OnDisable()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
