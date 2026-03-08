using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    public static CameraShake Instance => instance;

    private Vector3 originalPosition;
    private float shakeIntensity;
    private float totalDuration;
    private float remainingDuration;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    public void Shake(float intensity, float duration)
    {
        originalPosition = transform.localPosition;
        shakeIntensity = intensity;
        totalDuration = duration;
        remainingDuration = duration;
    }

    private void LateUpdate()
    {
        if (remainingDuration <= 0f) return;

        remainingDuration -= Time.deltaTime;

        if (remainingDuration <= 0f)
        {
            transform.localPosition = originalPosition;
            return;
        }

        float progress = remainingDuration / totalDuration;
        float currentIntensity = shakeIntensity * progress;
        float offsetX = Random.Range(-currentIntensity, currentIntensity);
        float offsetY = Random.Range(-currentIntensity, currentIntensity);
        transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);
    }
}
