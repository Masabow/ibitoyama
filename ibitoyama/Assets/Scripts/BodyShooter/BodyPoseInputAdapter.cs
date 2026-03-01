using System;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class BodyPoseInputAdapter : MonoBehaviour
  {
    private const int LeftShoulderIndex = 11;
    private const int RightShoulderIndex = 12;

    [SerializeField] private bool mirrorMode = false;
    [SerializeField] private bool invertY = true;
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float trackingTimeoutSec = 0.25f;

    public event Action<Vector2> OnChestNormalizedChanged;
    public event Action<bool> OnTrackingStateChanged;

    private Vector2 _targetChest = new Vector2(0.5f, 0.5f);
    private Vector2 _currentChest = new Vector2(0.5f, 0.5f);
    private Vector2 _velocity;

    private float _lastTrackedAt = -100f;
    private bool _isTracking;

    public Vector2 CurrentChestNormalized => _currentChest;
    public bool IsTracking => _isTracking;

    private void Update()
    {
      if (_isTracking && Time.unscaledTime - _lastTrackedAt > trackingTimeoutSec)
      {
        SetTrackingState(false);
      }

      var previous = _currentChest;
      _currentChest = new Vector2(
        Mathf.SmoothDamp(_currentChest.x, _targetChest.x, ref _velocity.x, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime),
        Mathf.SmoothDamp(_currentChest.y, _targetChest.y, ref _velocity.y, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime));

      if (Vector2.SqrMagnitude(previous - _currentChest) > 0.000001f)
      {
        OnChestNormalizedChanged?.Invoke(_currentChest);
      }
    }

    public void PushPoseResult(PoseLandmarkerResult result)
    {
      if (!TryGetChestCenter(result, out var chestCenter))
      {
        return;
      }

      _lastTrackedAt = Time.unscaledTime;
      SetTrackingState(true);

      var x = mirrorMode ? 1f - chestCenter.x : chestCenter.x;
      var y = invertY ? 1f - chestCenter.y : chestCenter.y;
      _targetChest = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    public void ResetCalibration()
    {
      _targetChest = new Vector2(0.5f, 0.5f);
      _currentChest = new Vector2(0.5f, 0.5f);
      _velocity = Vector2.zero;
    }

    private static bool TryGetChestCenter(PoseLandmarkerResult result, out Vector2 chest)
    {
      chest = new Vector2(0.5f, 0.5f);
      if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
      {
        return false;
      }

      var pose = result.poseLandmarks[0];
      if (pose.landmarks == null || pose.landmarks.Count <= RightShoulderIndex)
      {
        return false;
      }

      var leftShoulder = pose.landmarks[LeftShoulderIndex];
      var rightShoulder = pose.landmarks[RightShoulderIndex];
      chest = new Vector2((leftShoulder.x + rightShoulder.x) * 0.5f, (leftShoulder.y + rightShoulder.y) * 0.5f);
      return true;
    }

    private void SetTrackingState(bool isTracking)
    {
      if (_isTracking == isTracking)
      {
        return;
      }

      _isTracking = isTracking;
      OnTrackingStateChanged?.Invoke(_isTracking);
    }
  }
}
