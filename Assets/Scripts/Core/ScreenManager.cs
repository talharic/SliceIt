using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ScreenManager : MonoBehaviour, IScreenManager
{
    private const int REFERENCE_WIDTH = 1080;
    private const int REFERENCE_HEIGHT = 1920;
    private const float PLAY_AREA_WIDTH = 6f;
    private const float PLAY_AREA_HEIGHT = 10f;
    private const int MIN_SCREEN_DIMENSION = 1;

    public static ScreenManager Instance { get; private set; }

    public float PlayAreaWidth => PLAY_AREA_WIDTH;
    public float PlayAreaHeight => PLAY_AREA_HEIGHT;
    public Rect SafeAreaRect { get; private set; }
    public float ScaleFactor { get; private set; }
    public float AspectRatio { get; private set; }

    private int previousScreenWidth;
    private int previousScreenHeight;

    private void Awake()
    {
        if (!InitializeSingleton())
            return;

        RecalculateScreenMetrics();
        LogDeviceInfo();
    }

    private void Update()
    {
        if (HasResolutionChanged())
            RecalculateScreenMetrics();
    }

    private bool InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    private bool HasResolutionChanged()
    {
        return Screen.width != previousScreenWidth
            || Screen.height != previousScreenHeight;
    }

    private void RecalculateScreenMetrics()
    {
        int screenWidth = Mathf.Max(Screen.width, MIN_SCREEN_DIMENSION);
        int screenHeight = Mathf.Max(Screen.height, MIN_SCREEN_DIMENSION);

        ScaleFactor = screenWidth / (float)REFERENCE_WIDTH;
        AspectRatio = (float)screenWidth / screenHeight;
        SafeAreaRect = Screen.safeArea;

        previousScreenWidth = screenWidth;
        previousScreenHeight = screenHeight;
    }

    private void LogDeviceInfo()
    {
        Debug.Log(
            $"[ScreenManager] Resolution: {Screen.width}x{Screen.height} | "
          + $"DPI: {Screen.dpi:F0} | "
          + $"Aspect: {AspectRatio:F3} | "
          + $"Scale: {ScaleFactor:F2} | "
          + $"SafeArea: {SafeAreaRect}"
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
