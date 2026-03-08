using UnityEngine;

public class ResponsiveCamera : MonoBehaviour, IResponsiveCamera
{
    private const float PLAY_AREA_WIDTH = 6f;
    private const float PLAY_AREA_HEIGHT = 10f;
    private const float HALF_PLAY_AREA_WIDTH = PLAY_AREA_WIDTH / 2f;
    private const float HALF_PLAY_AREA_HEIGHT = PLAY_AREA_HEIGHT / 2f;

    private Camera targetCamera;
    private IScreenManager screenManager;
    private int previousScreenWidth;
    private int previousScreenHeight;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            Debug.LogError("[ResponsiveCamera] No Camera component found on this GameObject");
            enabled = false;
        }
    }

    private void Start()
    {
        screenManager = FindAnyObjectByType<ScreenManager>();
        if (screenManager == null)
        {
            Debug.LogError("[ResponsiveCamera] IScreenManager implementation not found in scene");
            enabled = false;
            return;
        }

        UpdateCameraSize();
    }

    private void Update()
    {
        if (HasResolutionChanged())
            UpdateCameraSize();
    }

    public void UpdateCameraSize()
    {
        float screenAspect = screenManager.AspectRatio;
        if (screenAspect <= 0f)
            return;

        targetCamera.orthographicSize = Mathf.Max(
            HALF_PLAY_AREA_HEIGHT,
            HALF_PLAY_AREA_WIDTH / screenAspect
        );

        previousScreenWidth = Screen.width;
        previousScreenHeight = Screen.height;
    }

    private bool HasResolutionChanged()
    {
        return Screen.width != previousScreenWidth
            || Screen.height != previousScreenHeight;
    }
}
