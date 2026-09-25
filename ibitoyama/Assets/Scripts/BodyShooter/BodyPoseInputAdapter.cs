using System;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class BodyPoseInputAdapter : MonoBehaviour
  {
    private const int LeftShoulderIndex = 11;
    private const int RightShoulderIndex = 12;
    private const int MaxSupportedPlayers = 4;

    [SerializeField] private bool mirrorMode = false;
    [SerializeField] private bool invertY = true;
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private float trackingTimeoutSec = 0.25f;
    [SerializeField] [Range(1, MaxSupportedPlayers)] private int maxPlayers = MaxSupportedPlayers;

    public event Action<int, Vector2> OnChestNormalizedChanged;
    public event Action<int, bool> OnTrackingStateChanged;

    private readonly Vector2[] _targetChests = new Vector2[MaxSupportedPlayers];
    private readonly Vector2[] _currentChests = new Vector2[MaxSupportedPlayers];
    private readonly Vector2[] _velocities = new Vector2[MaxSupportedPlayers];
    private readonly float[] _lastTrackedAt = new float[MaxSupportedPlayers];
    private readonly bool[] _trackingStates = new bool[MaxSupportedPlayers];
    private readonly bool[] _zoneUpdated = new bool[MaxSupportedPlayers];

    public int MaxPlayers => Mathf.Clamp(maxPlayers, 1, MaxSupportedPlayers);

    private void Update()
    {
      for (var playerIndex = 0; playerIndex < MaxPlayers; playerIndex++)
      {
        if (_trackingStates[playerIndex] && Time.unscaledTime - _lastTrackedAt[playerIndex] > trackingTimeoutSec)
        {
          SetTrackingState(playerIndex, false);
        }

        var previous = _currentChests[playerIndex];
        var velocity = _velocities[playerIndex];
        _currentChests[playerIndex] = new Vector2(
          Mathf.SmoothDamp(_currentChests[playerIndex].x, _targetChests[playerIndex].x, ref velocity.x, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime),
          Mathf.SmoothDamp(_currentChests[playerIndex].y, _targetChests[playerIndex].y, ref velocity.y, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime));
        _velocities[playerIndex] = velocity;

        if (Vector2.SqrMagnitude(previous - _currentChests[playerIndex]) > 0.000001f)
        {
          OnChestNormalizedChanged?.Invoke(playerIndex, _currentChests[playerIndex]);
        }
      }
    }

    public void PushPoseResult(PoseLandmarkerResult result)
    {
      // 画面（カメラフレーム）を横方向に MaxPlayers 個の固定ゾーンに分割し、
      // 各ゾーンに立った人物をそのままプレイヤー番号に割り当てる。
      //  - 画面左 1/4 に立つ人 → Player 0、その右 → Player 1 …
      //  - ゾーン内のローカル位置(0-1)を自機のレーン全幅にマッピングする
      // これにより検出順や人数に依存せず、物理的な立ち位置で位置が安定する。
      var zoneCount = MaxPlayers;
      var zoneWidth = 1f / zoneCount;
      Array.Clear(_zoneUpdated, 0, _zoneUpdated.Length);

      if (result.poseLandmarks != null && result.poseLandmarks.Count > 0)
      {
        var poseCount = result.poseLandmarks.Count;
        for (var poseIdx = 0; poseIdx < poseCount; poseIdx++)
        {
          if (!TryGetChestCenter(result, poseIdx, out var chest))
          {
            continue;
          }

          // 画面表示上のX（mirrorMode のときは画像右端が画面左に見えるため反転）
          var displayX = Mathf.Clamp01(mirrorMode ? 1f - chest.x : chest.x);

          // 立ち位置からゾーン（プレイヤー番号）を決定する
          var zone = Mathf.Clamp(Mathf.FloorToInt(displayX * zoneCount), 0, zoneCount - 1);
          if (_zoneUpdated[zone])
          {
            // 同一ゾーンに複数人 → 先に検出された1人を採用する
            continue;
          }

          // ゾーン内ローカルX(0-1)に正規化する
          var localX = (displayX - (zone * zoneWidth)) / zoneWidth;
          var y = invertY ? 1f - chest.y : chest.y;

          _targetChests[zone] = new Vector2(Mathf.Clamp01(localX), Mathf.Clamp01(y));
          _lastTrackedAt[zone] = Time.unscaledTime;
          SetTrackingState(zone, true);
          _zoneUpdated[zone] = true;
        }
      }

      // このフレームで人物が居なかったゾーンはトラッキング解除する
      for (var zone = 0; zone < zoneCount; zone++)
      {
        if (!_zoneUpdated[zone])
        {
          SetTrackingState(zone, false);
        }
      }
    }

    public void ResetCalibration()
    {
      for (var playerIndex = 0; playerIndex < MaxSupportedPlayers; playerIndex++)
      {
        _targetChests[playerIndex] = new Vector2(0.5f, 0.5f);
        _currentChests[playerIndex] = new Vector2(0.5f, 0.5f);
        _velocities[playerIndex] = Vector2.zero;
        _lastTrackedAt[playerIndex] = -100f;
        SetTrackingState(playerIndex, false);
      }
    }

    public Vector2 GetCurrentChestNormalized(int playerIndex)
    {
      return IsPlayerIndexValid(playerIndex) ? _currentChests[playerIndex] : new Vector2(0.5f, 0.5f);
    }

    public bool IsTrackingPlayer(int playerIndex)
    {
      return IsPlayerIndexValid(playerIndex) && _trackingStates[playerIndex];
    }

    private static bool TryGetChestCenter(PoseLandmarkerResult result, int playerIndex, out Vector2 chest)
    {
      chest = new Vector2(0.5f, 0.5f);
      if (result.poseLandmarks == null)
      {
        return false;
      }

      try
      {
        if (result.poseLandmarks.Count == 0)
        {
          return false;
        }

        if ((uint)playerIndex >= (uint)result.poseLandmarks.Count)
        {
          return false;
        }

        var pose = result.poseLandmarks[playerIndex];
        if (pose.landmarks == null
            || (uint)LeftShoulderIndex >= (uint)pose.landmarks.Count
            || (uint)RightShoulderIndex >= (uint)pose.landmarks.Count)
        {
          return false;
        }

        var leftShoulder = pose.landmarks[LeftShoulderIndex];
        var rightShoulder = pose.landmarks[RightShoulderIndex];
        chest = new Vector2((leftShoulder.x + rightShoulder.x) * 0.5f, (leftShoulder.y + rightShoulder.y) * 0.5f);
        return true;
      }
      catch (ArgumentOutOfRangeException)
      {
        // Pose result can be swapped while this frame is reading it.
        return false;
      }
    }

    private bool IsPlayerIndexValid(int playerIndex)
    {
      return (uint)playerIndex < (uint)MaxPlayers;
    }

    private void SetTrackingState(int playerIndex, bool isTracking)
    {
      if (!IsPlayerIndexValid(playerIndex) || _trackingStates[playerIndex] == isTracking)
      {
        return;
      }

      _trackingStates[playerIndex] = isTracking;
      OnTrackingStateChanged?.Invoke(playerIndex, isTracking);
    }
  }
}
