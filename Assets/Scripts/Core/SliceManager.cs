using UnityEngine;

public class SliceManager : MonoBehaviour
{
    [SerializeField] private LineRenderer cutLineRenderer;

    private const float LINE_FADE_DURATION = 0.5f;
    private const float LINE_WIDTH = 0.05f;
    private static readonly Color LineColor = new(1f, 1f, 1f, 0.8f);

    private Camera mainCamera;
    private Vector3 startPosition;
    private bool isCutting;
    private float lineDisappearTimer;

    private enum InputPhase { None, Began, Moved, Ended }

    private void Awake()
    {
        Debug.Log("[SliceManager] Awake called");
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[SliceManager] Main Camera not found");
            enabled = false;
            return;
        }

        if (cutLineRenderer == null)
        {
            Debug.LogError("[SliceManager] CutLineRenderer not assigned");
            enabled = false;
            return;
        }

        Debug.Log($"[SliceManager] Initialized — Camera: {mainCamera.name}, LineRenderer: {cutLineRenderer.name}");
        InitializeLineRenderer();
    }

    private void Update()
    {
        HandleInput();
        HandleLineFade();
    }

    private void InitializeLineRenderer()
    {
        cutLineRenderer.startWidth = LINE_WIDTH;
        cutLineRenderer.endWidth = LINE_WIDTH;
        cutLineRenderer.positionCount = 2;
        cutLineRenderer.startColor = LineColor;
        cutLineRenderer.endColor = LineColor;
        cutLineRenderer.enabled = false;
    }

    private void HandleInput()
    {
        if (!TryGetInputPosition(out Vector2 screenPosition, out InputPhase phase))
            return;

        switch (phase)
        {
            case InputPhase.Began:
                StartCut(screenPosition);
                break;
            case InputPhase.Moved:
                if (isCutting) UpdateCut(screenPosition);
                break;
            case InputPhase.Ended:
                if (isCutting) EndCut(screenPosition);
                break;
        }
    }

    private bool TryGetInputPosition(out Vector2 position, out InputPhase phase)
    {
        if (Input.touchCount > 0)
            return TryGetTouchInput(out position, out phase);

        return TryGetMouseInput(out position, out phase);
    }

    private bool TryGetTouchInput(out Vector2 position, out InputPhase phase)
    {
        Touch touch = Input.GetTouch(0);
        position = touch.position;
        phase = touch.phase switch
        {
            TouchPhase.Began => InputPhase.Began,
            TouchPhase.Moved or TouchPhase.Stationary => InputPhase.Moved,
            TouchPhase.Ended or TouchPhase.Canceled => InputPhase.Ended,
            _ => InputPhase.None
        };
        return phase != InputPhase.None;
    }

    private bool TryGetMouseInput(out Vector2 position, out InputPhase phase)
    {
        position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
            phase = InputPhase.Began;
        else if (Input.GetMouseButton(0))
            phase = InputPhase.Moved;
        else if (Input.GetMouseButtonUp(0))
            phase = InputPhase.Ended;
        else
        {
            phase = InputPhase.None;
            return false;
        }

        return true;
    }

    private void StartCut(Vector2 screenPosition)
    {
        isCutting = true;
        startPosition = ScreenToWorldPosition(screenPosition);
        cutLineRenderer.enabled = true;
        cutLineRenderer.SetPosition(0, startPosition);
        cutLineRenderer.SetPosition(1, startPosition);
        Debug.Log($"[SliceManager] Mouse Down at screen:{screenPosition} world:{startPosition}");
    }

    private void UpdateCut(Vector2 screenPosition)
    {
        Vector3 worldPos = ScreenToWorldPosition(screenPosition);
        cutLineRenderer.SetPosition(1, worldPos);
    }

    private void EndCut(Vector2 screenPosition)
    {
        isCutting = false;
        Vector3 endPosition = ScreenToWorldPosition(screenPosition);
        cutLineRenderer.SetPosition(1, endPosition);
        lineDisappearTimer = LINE_FADE_DURATION;

        TrySlice(startPosition, endPosition);
    }

    private void TrySlice(Vector3 cutStart, Vector3 cutEnd)
    {
        const int sampleCount = 10;
        float cameraZ = mainCamera.transform.position.z;

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 samplePoint = Vector3.Lerp(cutStart, cutEnd, t);
            var rayOrigin = new Vector3(samplePoint.x, samplePoint.y, cameraZ);

            if (!Physics.Raycast(rayOrigin, Vector3.forward, out RaycastHit hit, 100f))
                continue;

            var sliceable = hit.collider.GetComponent<SliceableObject>();
            if (sliceable == null || !sliceable.CanBeSliced)
                continue;

            sliceable.Slice(cutStart, cutEnd);
            return;
        }
    }

    private void HandleLineFade()
    {
        if (isCutting || !cutLineRenderer.enabled)
            return;

        lineDisappearTimer -= Time.deltaTime;
        if (lineDisappearTimer <= 0f)
            cutLineRenderer.enabled = false;
    }

    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z);
        var screenPoint = new Vector3(screenPosition.x, screenPosition.y, cameraDistance);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPoint);
        worldPosition.z = -1f;
        return worldPosition;
    }
}
