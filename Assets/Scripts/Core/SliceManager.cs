using UnityEngine;

public class SliceManager : MonoBehaviour
{
    [SerializeField] private LineRenderer cutLineRenderer;

    private const float LineFadeDuration = 0.5f;
    private const float LineWidthStart = 0.03f;
    private const float LineWidthEnd = 0.06f;
    private const float TargetRatio = 0.5f;
    private const float OnTargetThreshold = 0.15f;

    private static readonly Color DefaultLineColor = new(1f, 1f, 1f, 0.9f);
    private static readonly Color OnTargetLineColor = new(1f, 0.843f, 0f, 0.95f);

    private Camera mainCamera;
    private ScoreDisplay scoreDisplay;
    private LevelManager levelManager;
    private Vector3 startPosition;
    private bool isCutting;
    private float lineDisappearTimer;

    private enum InputPhase { None, Began, Moved, Ended }

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

        scoreDisplay = FindAnyObjectByType<ScoreDisplay>();
        levelManager = FindAnyObjectByType<LevelManager>();

        InitializeLineRenderer();
    }

    private void Update()
    {
        HandleInput();
        HandleLineFade();
    }

    private void InitializeLineRenderer()
    {
        cutLineRenderer.startWidth = LineWidthStart;
        cutLineRenderer.endWidth = LineWidthEnd;
        cutLineRenderer.positionCount = 2;
        cutLineRenderer.startColor = DefaultLineColor;
        cutLineRenderer.endColor = DefaultLineColor;
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

    private static bool TryGetTouchInput(out Vector2 position, out InputPhase phase)
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

    private static bool TryGetMouseInput(out Vector2 position, out InputPhase phase)
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
        SetLineColor(DefaultLineColor);
    }

    private void UpdateCut(Vector2 screenPosition)
    {
        Vector3 worldPos = ScreenToWorldPosition(screenPosition);
        cutLineRenderer.SetPosition(1, worldPos);
        UpdateLineColorByProximity(startPosition, worldPos);
    }

    private void EndCut(Vector2 screenPosition)
    {
        isCutting = false;
        Vector3 endPosition = ScreenToWorldPosition(screenPosition);
        cutLineRenderer.SetPosition(1, endPosition);
        lineDisappearTimer = LineFadeDuration;

        TrySlice(startPosition, endPosition);
    }

    private void UpdateLineColorByProximity(Vector3 cutStart, Vector3 cutEnd)
    {
        var sliceable = FindActiveSliceable();
        if (sliceable == null)
        {
            SetLineColor(DefaultLineColor);
            return;
        }

        var targetLine = sliceable.GetComponent<TargetLineDisplay>();
        if (targetLine == null)
        {
            SetLineColor(DefaultLineColor);
            return;
        }

        Bounds bounds = sliceable.GetComponent<Renderer>().bounds;
        float targetY = bounds.min.y + targetLine.TargetRatio * bounds.size.y;

        Vector3 cutMidpoint = (cutStart + cutEnd) * 0.5f;
        float normalizedDistance = Mathf.Abs(cutMidpoint.y - targetY) / bounds.size.y;

        Color lineColor = normalizedDistance < OnTargetThreshold
            ? Color.Lerp(OnTargetLineColor, DefaultLineColor, normalizedDistance / OnTargetThreshold)
            : DefaultLineColor;

        SetLineColor(lineColor);
    }

    private static SliceableObject FindActiveSliceable()
    {
        return Object.FindAnyObjectByType<SliceableObject>();
    }

    private void SetLineColor(Color color)
    {
        cutLineRenderer.startColor = color;
        cutLineRenderer.endColor = color;
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

            Bounds objectBounds = hit.collider.bounds;
            Vector3 cutNormal = Vector3.Cross((cutEnd - cutStart).normalized, Vector3.forward).normalized;
            bool sliceSucceeded = sliceable.Slice(cutStart, cutEnd);

            if (sliceSucceeded)
            {
                ScoreResult baseScore = ScoreCalculator.CalculateScore(
                    cutStart, cutEnd, objectBounds, TargetRatio);
                Vector3 scoreMidpoint = (cutStart + cutEnd) * 0.5f;

                ScoreResult finalScore = ApplyComboMultiplier(baseScore);

                scoreDisplay?.ShowScore(finalScore, scoreMidpoint);
                levelManager?.OnSliceComplete(finalScore);

                PlayMaterialEffects(sliceable, scoreMidpoint, cutNormal);

                if (finalScore.Grade == ScoreGrade.Perfect)
                    ScreenFlash.Instance?.PlayFlash();
            }

            return;
        }
    }

    private static ScoreResult ApplyComboMultiplier(ScoreResult baseScore)
    {
        if (ComboSystem.Instance == null)
            return baseScore;

        float comboMultiplier = ComboSystem.Instance.RegisterCut(baseScore.Grade);
        int comboPoints = Mathf.RoundToInt(baseScore.Points * comboMultiplier);

        return new ScoreResult(
            baseScore.ActualRatio,
            baseScore.Deviation,
            baseScore.Grade,
            baseScore.Multiplier,
            comboPoints);
    }

    private static void PlayMaterialEffects(SliceableObject sliceable, Vector3 position, Vector3 cutNormal)
    {
        MaterialData materialData = sliceable.CurrentMaterialData;

        if (materialData != null)
        {
            SoundManager.Instance?.PlaySliceSound(materialData.Type);
            SliceEffects.Instance?.PlaySliceEffect(
                position, cutNormal, materialData.ParticleCount, materialData.ParticleColor);
        }
        else
        {
            SoundManager.Instance?.PlaySliceSound();
            SliceEffects.Instance?.PlaySliceEffect(position, cutNormal);
        }

        HapticManager.Instance?.PlaySliceHaptic();
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
