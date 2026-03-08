using UnityEngine;

public interface IScreenManager
{
    float PlayAreaWidth { get; }
    float PlayAreaHeight { get; }
    Rect SafeAreaRect { get; }
    float ScaleFactor { get; }
    float AspectRatio { get; }
}
