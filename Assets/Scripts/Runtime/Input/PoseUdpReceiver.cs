using System;
using UnityEngine;

/// <summary>
/// Receives pose-like input from direct webcam sampling or local mock,
/// then triggers a simple hands-up action.
/// </summary>
public class PoseUdpReceiver : MonoBehaviour
{
    public enum PoseInputMode
    {
        WebcamDirect,
        MediaPipeExternal,
        LocalMock
    }

    [Header("Input Source")]
    public PoseInputMode inputMode = PoseInputMode.WebcamDirect;

    [Header("Webcam")]
    public int webcamWidth = 640;
    public int webcamHeight = 480;
    public int webcamFps = 30;
    [Range(0.01f, 0.5f)]
    public float motionSensitivity = 0.08f;
    public bool showWebcamPreview = true;
    [Range(0.1f, 1.0f)]
    public float previewScale = 0.33f;
    public Vector2 previewMargin = new Vector2(16.0f, 16.0f);
    public bool showPoseBoxes = true;
    public Color faceBoxColor = new Color(0.1f, 1.0f, 0.2f, 1.0f);
    public Color bodyBoxColor = new Color(1.0f, 0.8f, 0.1f, 1.0f);

    [Header("Filtering")]
    [Range(0.0f, 1.0f)]
    public float confidenceThreshold = 0.5f;

    [Range(0.0f, 1.0f)]
    public float smoothing = 0.35f;

    [Header("Local Mock")]
    public float mockCycleSeconds = 4.0f;

    private WebCamTexture _webcam;
    private Color32[] _frameBuffer;
    private byte[] _prevLuma;
    private bool _hasPrevFrame;

    private Vector2 _leftWrist;
    private Vector2 _rightWrist;
    private Vector2 _nose;
    private bool _barrierActive;

    private readonly float[] _leftKp = new float[3];
    private readonly float[] _rightKp = new float[3];
    private readonly float[] _noseKp = new float[3];
    private Texture2D _lineTexture;
    private float _lastExternalPoseTime = -999.0f;
    private bool _externalPoseWarned;

    private void Start()
    {
        if (inputMode == PoseInputMode.WebcamDirect)
        {
            StartWebcam();
        }
        else
        {
            Debug.Log("PoseUdpReceiver running with local mock input");
        }
    }

    private void Update()
    {
        if (inputMode == PoseInputMode.WebcamDirect)
        {
            ApplyWebcamHeuristic();
        }
        else if (inputMode == PoseInputMode.MediaPipeExternal)
        {
            if (Time.time - _lastExternalPoseTime > 1.0f && !_externalPoseWarned)
            {
                Debug.LogWarning("MediaPipeExternal mode is active but no external pose data has been received yet.");
                _externalPoseWarned = true;
            }
        }
        else
        {
            ApplyLocalMock();
        }

        bool handsUp = _leftWrist.y < _nose.y && _rightWrist.y < _nose.y;
        if (handsUp != _barrierActive)
        {
            _barrierActive = handsUp;
            Debug.Log(_barrierActive ? "Barrier ON" : "Barrier OFF");
        }
    }

    private void OnDestroy()
    {
        if (_webcam != null && _webcam.isPlaying)
        {
            _webcam.Stop();
        }

        if (_lineTexture != null)
        {
            Destroy(_lineTexture);
            _lineTexture = null;
        }
    }

    private void OnGUI()
    {
        if (!showWebcamPreview || inputMode != PoseInputMode.WebcamDirect)
        {
            return;
        }

        if (_webcam == null || !_webcam.isPlaying || _webcam.width <= 16 || _webcam.height <= 16)
        {
            return;
        }

        float width = Mathf.Min(Screen.width * previewScale, _webcam.width);
        float aspect = (float)_webcam.height / _webcam.width;
        float height = width * aspect;
        var previewRect = new Rect(previewMargin.x, previewMargin.y, width, height);
        GUI.DrawTexture(previewRect, _webcam, ScaleMode.ScaleToFit, false);

        if (!showPoseBoxes)
        {
            return;
        }

        EnsureLineTexture();
        var faceRect = BuildFaceRect();
        var bodyRect = BuildBodyRect(faceRect);
        DrawRectOutline(NormalizedToPreview(faceRect, previewRect), faceBoxColor, 2.0f);
        DrawRectOutline(NormalizedToPreview(bodyRect, previewRect), bodyBoxColor, 2.0f);
    }

    private void StartWebcam()
    {
        if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("No webcam device found. Falling back to local mock.");
            inputMode = PoseInputMode.LocalMock;
            return;
        }

        _webcam = new WebCamTexture(WebCamTexture.devices[0].name, webcamWidth, webcamHeight, webcamFps);
        _webcam.Play();
        _hasPrevFrame = false;
        Debug.Log($"PoseUdpReceiver using webcam: {_webcam.deviceName}");
    }

    private void ApplyWebcamHeuristic()
    {
        if (_webcam == null || !_webcam.isPlaying || !_webcam.didUpdateThisFrame)
        {
            return;
        }

        int width = _webcam.width;
        int height = _webcam.height;
        int pixelCount = width * height;
        if (pixelCount <= 0)
        {
            return;
        }

        if (_frameBuffer == null || _frameBuffer.Length != pixelCount)
        {
            _frameBuffer = new Color32[pixelCount];
            _prevLuma = new byte[pixelCount];
            _hasPrevFrame = false;
        }

        _webcam.GetPixels32(_frameBuffer);

        int step = 3;
        float sensitivity01 = Mathf.InverseLerp(0.01f, 0.5f, motionSensitivity);
        int deltaThreshold = Mathf.RoundToInt(Mathf.Lerp(32.0f, 10.0f, sensitivity01));
        int minMotionCount = Mathf.RoundToInt(Mathf.Lerp(90.0f, 24.0f, sensitivity01));
        int motionCount = 0;

        int minX = width;
        int maxX = -1;
        int minY = height;
        int maxY = -1;

        int leftCount = 0;
        int rightCount = 0;
        float leftSumX = 0.0f;
        float leftSumY = 0.0f;
        float rightSumX = 0.0f;
        float rightSumY = 0.0f;

        float centerX = width * 0.5f;

        for (int y = 0; y < height; y += step)
        {
            int row = y * width;
            for (int x = 0; x < width; x += step)
            {
                int idx = row + x;
                Color32 c = _frameBuffer[idx];
                byte luma = (byte)((77 * c.r + 150 * c.g + 29 * c.b) >> 8);

                if (_hasPrevFrame)
                {
                    int delta = Mathf.Abs(luma - _prevLuma[idx]);
                    if (delta >= deltaThreshold)
                    {
                        motionCount++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;

                        if (x < centerX)
                        {
                            leftCount++;
                            leftSumX += x;
                            leftSumY += y;
                        }
                        else
                        {
                            rightCount++;
                            rightSumX += x;
                            rightSumY += y;
                        }
                    }
                }

                _prevLuma[idx] = luma;
            }
        }

        _hasPrevFrame = true;
        if (motionCount < minMotionCount || maxX <= minX || maxY <= minY)
        {
            return;
        }

        int pad = 8;
        minX = Mathf.Max(0, minX - pad);
        maxX = Mathf.Min(width - 1, maxX + pad);
        minY = Mathf.Max(0, minY - pad);
        maxY = Mathf.Min(height - 1, maxY + pad);

        float xMinNorm = minX / (float)width;
        float xMaxNorm = maxX / (float)width;
        float yTopNorm = 1.0f - (maxY / (float)height);
        float yBottomNorm = 1.0f - (minY / (float)height);

        var bodyRect = ClampNormalizedRect(Rect.MinMaxRect(xMinNorm, yTopNorm, xMaxNorm, yBottomNorm));
        float faceWidth = Mathf.Clamp(bodyRect.width * 0.42f, 0.10f, 0.30f);
        float faceHeight = Mathf.Clamp(bodyRect.height * 0.30f, 0.12f, 0.30f);
        var faceRect = ClampNormalizedRect(new Rect(
            bodyRect.center.x - faceWidth * 0.5f,
            bodyRect.y + bodyRect.height * 0.03f,
            faceWidth,
            faceHeight));

        float leftXNorm;
        float leftYNorm;
        if (leftCount > 0)
        {
            leftXNorm = (leftSumX / leftCount) / width;
            leftYNorm = 1.0f - ((leftSumY / leftCount) / height);
        }
        else
        {
            leftXNorm = bodyRect.xMin + bodyRect.width * 0.25f;
            leftYNorm = bodyRect.yMin + bodyRect.height * 0.65f;
        }

        float rightXNorm;
        float rightYNorm;
        if (rightCount > 0)
        {
            rightXNorm = (rightSumX / rightCount) / width;
            rightYNorm = 1.0f - ((rightSumY / rightCount) / height);
        }
        else
        {
            rightXNorm = bodyRect.xMin + bodyRect.width * 0.75f;
            rightYNorm = bodyRect.yMin + bodyRect.height * 0.65f;
        }

        _leftKp[0] = Mathf.Clamp01(leftXNorm);
        _leftKp[1] = Mathf.Clamp01(leftYNorm);
        _leftKp[2] = 0.9f;

        _rightKp[0] = Mathf.Clamp01(rightXNorm);
        _rightKp[1] = Mathf.Clamp01(rightYNorm);
        _rightKp[2] = 0.9f;

        _noseKp[0] = faceRect.center.x;
        _noseKp[1] = faceRect.y + faceRect.height * 0.55f;
        _noseKp[2] = 0.9f;

        TryUpdate(ref _leftWrist, _leftKp);
        TryUpdate(ref _rightWrist, _rightKp);
        TryUpdate(ref _nose, _noseKp);
    }

    private void ApplyLocalMock()
    {
        float cycle = Mathf.Max(1.0f, mockCycleSeconds);
        float phase = (Time.time % cycle) / cycle;
        float baseX = 0.5f + 0.06f * Mathf.Sin(phase * 2.0f * Mathf.PI);
        float noseY = 0.25f;

        bool handsUp = phase >= 0.35f && phase <= 0.55f;
        float leftY = handsUp ? noseY - 0.06f : 0.48f + 0.02f * Mathf.Sin(phase * 6.0f * Mathf.PI);
        float rightY = handsUp ? noseY - 0.05f : 0.48f + 0.02f * Mathf.Cos(phase * 6.0f * Mathf.PI);

        TryUpdate(ref _leftWrist, new[] { baseX - 0.16f, leftY, 0.95f });
        TryUpdate(ref _rightWrist, new[] { baseX + 0.16f, rightY, 0.95f });
        TryUpdate(ref _nose, new[] { baseX, noseY, 0.98f });
    }

    private void TryUpdate(ref Vector2 current, float[] kp)
    {
        if (kp == null || kp.Length < 3)
        {
            return;
        }

        float confidence = kp[2];
        if (confidence < confidenceThreshold)
        {
            return;
        }

        var next = new Vector2(kp[0], kp[1]);
        current = Vector2.Lerp(current, next, smoothing);
    }

    public void ApplyExternalPose(float noseX, float noseY, float noseConfidence, float leftWristX, float leftWristY, float leftWristConfidence, float rightWristX, float rightWristY, float rightWristConfidence)
    {
        _noseKp[0] = noseX;
        _noseKp[1] = noseY;
        _noseKp[2] = noseConfidence;
        _leftKp[0] = leftWristX;
        _leftKp[1] = leftWristY;
        _leftKp[2] = leftWristConfidence;
        _rightKp[0] = rightWristX;
        _rightKp[1] = rightWristY;
        _rightKp[2] = rightWristConfidence;

        TryUpdate(ref _nose, _noseKp);
        TryUpdate(ref _leftWrist, _leftKp);
        TryUpdate(ref _rightWrist, _rightKp);
        _lastExternalPoseTime = Time.time;
        _externalPoseWarned = false;
    }

    private void EnsureLineTexture()
    {
        if (_lineTexture != null)
        {
            return;
        }

        _lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _lineTexture.SetPixel(0, 0, Color.white);
        _lineTexture.Apply();
    }

    private Rect BuildFaceRect()
    {
        float faceWidth = 0.14f;
        float faceHeight = 0.18f;
        float x = _nose.x - faceWidth * 0.5f;
        float y = _nose.y - faceHeight * 0.55f;
        return ClampNormalizedRect(new Rect(x, y, faceWidth, faceHeight));
    }

    private Rect BuildBodyRect(Rect faceRect)
    {
        float minX = Mathf.Min(_leftWrist.x, _rightWrist.x) - 0.07f;
        float maxX = Mathf.Max(_leftWrist.x, _rightWrist.x) + 0.07f;
        float topY = faceRect.yMax + 0.02f;
        float bottomY = Mathf.Max(_leftWrist.y, _rightWrist.y) + 0.30f;
        var rect = new Rect(minX, topY, maxX - minX, Mathf.Max(0.18f, bottomY - topY));
        return ClampNormalizedRect(rect);
    }

    private static Rect ClampNormalizedRect(Rect rect)
    {
        float xMin = Mathf.Clamp01(rect.xMin);
        float yMin = Mathf.Clamp01(rect.yMin);
        float xMax = Mathf.Clamp01(rect.xMax);
        float yMax = Mathf.Clamp01(rect.yMax);
        return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin + 0.01f, xMax), Mathf.Max(yMin + 0.01f, yMax));
    }

    private static Rect NormalizedToPreview(Rect normalized, Rect previewRect)
    {
        return new Rect(
            previewRect.x + normalized.x * previewRect.width,
            previewRect.y + normalized.y * previewRect.height,
            normalized.width * previewRect.width,
            normalized.height * previewRect.height);
    }

    private void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        if (_lineTexture == null || rect.width <= 0.0f || rect.height <= 0.0f)
        {
            return;
        }

        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), _lineTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), _lineTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), _lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), _lineTexture);
        GUI.color = old;
    }
}
