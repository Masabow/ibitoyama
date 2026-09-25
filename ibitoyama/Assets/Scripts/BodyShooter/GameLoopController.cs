using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ibitoyama.BodyShooter
{
  public class GameLoopController : MonoBehaviour
  {
    private const float PlayAreaXMin = -7f;
    private const float PlayAreaXMax = 7f;
    private const float PlayAreaYMin = -4.5f;
    private const float PlayAreaYMax = 4.5f;

    [SerializeField] private PlayerHealth[] playerHealths = Array.Empty<PlayerHealth>();
    [SerializeField] private PlayerShipController2D[] playerShips = Array.Empty<PlayerShipController2D>();
    [SerializeField] private EnemySpawner2D enemySpawner;
    [SerializeField] private BodyPoseInputAdapter bodyPoseInput;

    [Header("UI")]
    [SerializeField] private Text[] hpTexts = Array.Empty<Text>();
    [SerializeField] private Text[] trackingTexts = Array.Empty<Text>();
    [SerializeField] private GameObject gameOverPanel;

    public bool IsRunning { get; private set; }

    private bool _eventsBound;
    private readonly bool[] _trackingStates = new bool[4];
    private Action<int>[] _hpChangedHandlers = Array.Empty<Action<int>>();
    private Action[] _deadHandlers = Array.Empty<Action>();

    private void Awake()
    {
      if (playerHealths == null || playerHealths.Length == 0)
      {
        playerHealths = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
      }

      if (playerShips == null || playerShips.Length == 0)
      {
        playerShips = FindObjectsByType<PlayerShipController2D>(FindObjectsSortMode.None);
      }

      if (enemySpawner == null)
      {
        enemySpawner = FindFirstObjectByType<EnemySpawner2D>();
      }

      if (bodyPoseInput == null)
      {
        bodyPoseInput = FindFirstObjectByType<BodyPoseInputAdapter>();
      }
    }

    public void Configure(
      PlayerHealth[] healths,
      PlayerShipController2D[] ships,
      EnemySpawner2D spawner,
      BodyPoseInputAdapter poseInput,
      Text[] hp,
      Text[] tracking,
      GameObject gameOver)
    {
      playerHealths = healths ?? Array.Empty<PlayerHealth>();
      playerShips = ships ?? Array.Empty<PlayerShipController2D>();
      enemySpawner = spawner;
      bodyPoseInput = poseInput;
      hpTexts = hp ?? Array.Empty<Text>();
      trackingTexts = tracking ?? Array.Empty<Text>();
      gameOverPanel = gameOver;

      RebindEvents();
    }

    private void OnEnable()
    {
      RebindEvents();
    }

    private void OnDisable()
    {
      UnbindEvents();
    }

    private void Start()
    {
      IsRunning = true;
      if (gameOverPanel != null)
      {
        gameOverPanel.SetActive(false);
      }

      for (var i = 0; i < playerHealths.Length; i++)
      {
        playerHealths[i]?.ResetHealth();
      }

      if (enemySpawner != null)
      {
        enemySpawner.SetGameLoop(this);
        enemySpawner.ResetSpawner();
      }

      if (bodyPoseInput != null)
      {
        bodyPoseInput.ResetCalibration();
        for (var i = 0; i < playerShips.Length; i++)
        {
          OnTrackingStateChanged(i, bodyPoseInput.IsTrackingPlayer(i));
          OnChestNormalizedChanged(i, bodyPoseInput.GetCurrentChestNormalized(i));
        }
      }
      else
      {
        for (var i = 0; i < playerShips.Length; i++)
        {
          OnTrackingStateChanged(i, false);
        }
      }

      RecalculatePlayerLanes();
    }

    public void RestartGame()
    {
      Time.timeScale = 1f;
      var scene = SceneManager.GetActiveScene();
      SceneManager.LoadScene(scene.buildIndex);
    }

    private void OnChestNormalizedChanged(int playerIndex, Vector2 chest)
    {
      if (!IsRunning || !IsValidPlayerIndex(playerIndex) || playerShips[playerIndex] == null)
      {
        return;
      }

      playerShips[playerIndex].SetChestNormalized(chest);
    }

    private void OnTrackingStateChanged(int playerIndex, bool isTracking)
    {
      if ((uint)playerIndex < (uint)_trackingStates.Length)
      {
        _trackingStates[playerIndex] = isTracking;
      }

      if (trackingTexts != null && playerIndex < trackingTexts.Length && trackingTexts[playerIndex] != null)
      {
        trackingTexts[playerIndex].text = $"P{playerIndex + 1}: {(isTracking ? "Tracking" : "Lost")}";
      }

      if (!IsValidPlayerIndex(playerIndex) || playerShips[playerIndex] == null)
      {
        return;
      }

      playerShips[playerIndex].SetTrackingState(isTracking && IsRunning && !playerHealths[playerIndex].IsDead);
      RecalculatePlayerLanes();
    }

    private void OnHpChanged(int playerIndex, int hp)
    {
      if (hpTexts != null && playerIndex < hpTexts.Length && hpTexts[playerIndex] != null)
      {
        hpTexts[playerIndex].text = $"P{playerIndex + 1} HP: {hp}";
      }
    }

    private void OnDead(int playerIndex)
    {
      if (IsValidPlayerIndex(playerIndex) && playerShips[playerIndex] != null)
      {
        playerShips[playerIndex].SetTrackingState(false);
      }

      if (playerHealths.Any(health => health != null && !health.IsDead))
      {
        return;
      }

      IsRunning = false;
      if (gameOverPanel != null)
      {
        gameOverPanel.SetActive(true);
      }

      for (var i = 0; i < playerShips.Length; i++)
      {
        if (playerShips[i] != null)
        {
          playerShips[i].SetTrackingState(false);
        }
      }
    }

    private void RebindEvents()
    {
      UnbindEvents();

      _hpChangedHandlers = new Action<int>[playerHealths.Length];
      _deadHandlers = new Action[playerHealths.Length];

      for (var i = 0; i < playerHealths.Length; i++)
      {
        var health = playerHealths[i];
        if (health == null)
        {
          continue;
        }

        var playerIndex = i;
        _hpChangedHandlers[i] = hp => OnHpChanged(playerIndex, hp);
        _deadHandlers[i] = () => OnDead(playerIndex);
        health.OnHpChanged += _hpChangedHandlers[i];
        health.OnDead += _deadHandlers[i];
        OnHpChanged(playerIndex, health.CurrentHp);
      }

      if (bodyPoseInput != null)
      {
        bodyPoseInput.OnChestNormalizedChanged += OnChestNormalizedChanged;
        bodyPoseInput.OnTrackingStateChanged += OnTrackingStateChanged;
      }

      _eventsBound = true;
    }

    private void UnbindEvents()
    {
      if (!_eventsBound)
      {
        return;
      }

      for (var i = 0; i < playerHealths.Length; i++)
      {
        var health = playerHealths[i];
        if (health == null)
        {
          continue;
        }

        if (i < _hpChangedHandlers.Length && _hpChangedHandlers[i] != null)
        {
          health.OnHpChanged -= _hpChangedHandlers[i];
        }

        if (i < _deadHandlers.Length && _deadHandlers[i] != null)
        {
          health.OnDead -= _deadHandlers[i];
        }
      }

      if (bodyPoseInput != null)
      {
        bodyPoseInput.OnChestNormalizedChanged -= OnChestNormalizedChanged;
        bodyPoseInput.OnTrackingStateChanged -= OnTrackingStateChanged;
      }

      _eventsBound = false;
      _hpChangedHandlers = Array.Empty<Action<int>>();
      _deadHandlers = Array.Empty<Action>();
    }

    private bool IsValidPlayerIndex(int playerIndex)
    {
      return playerHealths != null
             && playerShips != null
             && (uint)playerIndex < (uint)playerHealths.Length
             && (uint)playerIndex < (uint)playerShips.Length
             && playerHealths[playerIndex] != null;
    }

    private void RecalculatePlayerLanes()
    {
      if (playerShips == null || playerShips.Length == 0)
      {
        return;
      }

      // 4人同時プレイ用にプレイエリアを固定で横方向に等分割する。
      // プレイヤー番号 = 入力側のゾーン番号なので、人数やトラッキング状態に
      // 関係なく各自のレーンは常に同じ位置に固定される。
      var laneCount = playerShips.Length;
      var laneWidth = (PlayAreaXMax - PlayAreaXMin) / laneCount;
      for (var i = 0; i < playerShips.Length; i++)
      {
        var ship = playerShips[i];
        if (ship == null)
        {
          continue;
        }

        var laneMin = PlayAreaXMin + (laneWidth * i);
        var laneMax = laneMin + laneWidth;
        ship.ConfigureBounds(laneMin, laneMax, PlayAreaYMin, PlayAreaYMax);
      }
    }
  }
}
