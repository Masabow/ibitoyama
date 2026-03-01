using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ibitoyama.BodyShooter
{
  public class GameLoopController : MonoBehaviour
  {
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerShipController2D playerShip;
    [SerializeField] private EnemySpawner2D enemySpawner;
    [SerializeField] private BodyPoseInputAdapter bodyPoseInput;

    [Header("UI")]
    [SerializeField] private Text hpText;
    [SerializeField] private Text trackingText;
    [SerializeField] private GameObject gameOverPanel;

    public bool IsRunning { get; private set; }
    private bool _eventsBound;

    private void Awake()
    {
      if (playerHealth == null)
      {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
      }

      if (playerShip == null)
      {
        playerShip = FindFirstObjectByType<PlayerShipController2D>();
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
      PlayerHealth health,
      PlayerShipController2D ship,
      EnemySpawner2D spawner,
      BodyPoseInputAdapter poseInput,
      Text hp,
      Text tracking,
      GameObject gameOver)
    {
      playerHealth = health;
      playerShip = ship;
      enemySpawner = spawner;
      bodyPoseInput = poseInput;
      hpText = hp;
      trackingText = tracking;
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

      if (playerHealth != null)
      {
        playerHealth.ResetHealth();
      }

      if (enemySpawner != null)
      {
        enemySpawner.SetGameLoop(this);
        enemySpawner.ResetSpawner();
      }

      if (bodyPoseInput != null)
      {
        bodyPoseInput.ResetCalibration();
        OnTrackingStateChanged(bodyPoseInput.IsTracking);
        OnChestNormalizedChanged(bodyPoseInput.CurrentChestNormalized);
      }
      else
      {
        OnTrackingStateChanged(false);
      }
    }

    public void RestartGame()
    {
      Time.timeScale = 1f;
      var scene = SceneManager.GetActiveScene();
      SceneManager.LoadScene(scene.buildIndex);
    }

    private void OnChestNormalizedChanged(Vector2 chest)
    {
      if (playerShip == null || !IsRunning)
      {
        return;
      }

      playerShip.SetChestNormalized(chest);
    }

    private void OnTrackingStateChanged(bool isTracking)
    {
      if (trackingText != null)
      {
        trackingText.text = isTracking ? "Tracking" : "Lost";
      }

      if (playerShip != null)
      {
        playerShip.SetTrackingState(isTracking && IsRunning);
      }
    }

    private void OnHpChanged(int hp)
    {
      if (hpText != null)
      {
        hpText.text = $"HP: {hp}";
      }
    }

    private void OnDead()
    {
      IsRunning = false;
      if (gameOverPanel != null)
      {
        gameOverPanel.SetActive(true);
      }

      if (playerShip != null)
      {
        playerShip.SetTrackingState(false);
      }
    }

    private void RebindEvents()
    {
      UnbindEvents();

      if (playerHealth != null)
      {
        playerHealth.OnHpChanged += OnHpChanged;
        playerHealth.OnDead += OnDead;
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

      if (playerHealth != null)
      {
        playerHealth.OnHpChanged -= OnHpChanged;
        playerHealth.OnDead -= OnDead;
      }

      if (bodyPoseInput != null)
      {
        bodyPoseInput.OnChestNormalizedChanged -= OnChestNormalizedChanged;
        bodyPoseInput.OnTrackingStateChanged -= OnTrackingStateChanged;
      }

      _eventsBound = false;
    }
  }
}
