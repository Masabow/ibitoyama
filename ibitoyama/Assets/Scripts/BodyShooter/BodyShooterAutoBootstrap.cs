using Mediapipe.Unity;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ibitoyama.BodyShooter
{
  public static class BodyShooterAutoBootstrap
  {
    private const string TargetSceneName = "BodyShooter2D";
    private const string PlayerObjectNamePrefix = "BodyShooterPlayer";
    private const string LegacyPlayerObjectName = "BodyShooterPlayer";
    private const string EnemyTemplateName = "BodyShooterEnemyTemplate";
    private const string GameplayCameraName = "BodyShooter Camera";
    private const string HudCanvasName = "BodyShooter HUD Canvas";
    private const string LaneDividerObjectName = "BodyShooter Lane Dividers";
    private const int MaxPlayers = 4;

    // プレイエリアの横方向の範囲（ゾーン分割・レーン分割の基準）
    private const float PlayAreaXMin = -7f;
    private const float PlayAreaXMax = 7f;
    private const float PlayAreaYMin = -4.5f;
    private const float PlayAreaYMax = 4.5f;

    // ポーズ（骨格）だけ表示し、カメラ映像を隠す場合は false にする
    private const bool ShowCameraImage = false;

    private static readonly Vector3[] PlayerPositions =
    {
      new Vector3(-5.25f, -4f, 0f),
      new Vector3(-1.75f, -4f, 0f),
      new Vector3(1.75f, -4f, 0f),
      new Vector3(5.25f, -4f, 0f),
    };

    private static readonly Vector2[] PlayerLaneBounds =
    {
      new Vector2(-7f, -3.5f),
      new Vector2(-3.5f, 0f),
      new Vector2(0f, 3.5f),
      new Vector2(3.5f, 7f),
    };

    private static readonly Color[] PlayerColors =
    {
      new Color(0.2f, 0.8f, 1f, 1f),
      new Color(0.3f, 1f, 0.4f, 1f),
      new Color(1f, 0.85f, 0.25f, 1f),
      new Color(1f, 0.45f, 0.8f, 1f),
    };

    private static bool _initializedForScene;
    private static Mesh _fallbackQuadMesh;
    private static Material _enemyMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitOnLoad()
    {
      SceneManager.sceneLoaded += OnSceneLoaded;
      TrySetup(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      _initializedForScene = false;
      TrySetup(scene);
    }

    private static void TrySetup(Scene scene)
    {
      if (_initializedForScene || scene.name != TargetSceneName)
      {
        return;
      }

      _initializedForScene = true;
      SetupScene();
    }

    private static void SetupScene()
    {
      var poseInput = Object.FindFirstObjectByType<BodyPoseInputAdapter>();
      if (poseInput == null)
      {
        poseInput = new GameObject("BodyPoseInput").AddComponent<BodyPoseInputAdapter>();
      }

      var runner = Object.FindFirstObjectByType<PoseLandmarkerRunner>();
      if (runner != null)
      {
        var bridge = runner.GetComponent<PoseResultBridge>() ?? runner.gameObject.AddComponent<PoseResultBridge>();
        bridge.Configure(poseInput, runner);
      }
      else
      {
        Debug.LogWarning("BodyShooter: PoseLandmarkerRunner not found. Keyboard fallback will be used.");
      }

      ApplyCameraImageVisibility();

      var players = CreateOrFindPlayers();
      var enemyTemplate = CreateOrFindEnemyTemplate();
      var spawner = Object.FindFirstObjectByType<EnemySpawner2D>();
      if (spawner == null)
      {
        spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner2D>();
      }
      spawner.Configure(enemyTemplate, -7f, 7f, 5.5f);

      EnsureMainCameraForGameplayAndUi();
      EnsureLaneDividers();
      EnsureEventSystem();
      CreateOrFindUi(out var hpTexts, out var trackingTexts, out var gameOverPanel, out var restartButton);

      var loop = Object.FindFirstObjectByType<GameLoopController>();
      if (loop == null)
      {
        loop = new GameObject("GameLoop").AddComponent<GameLoopController>();
      }

      var healths = new PlayerHealth[players.Length];
      var ships = new PlayerShipController2D[players.Length];
      for (var i = 0; i < players.Length; i++)
      {
        healths[i] = players[i].GetComponent<PlayerHealth>();
        ships[i] = players[i].GetComponent<PlayerShipController2D>();
      }

      loop.Configure(healths, ships, spawner, poseInput, hpTexts, trackingTexts, gameOverPanel);

      restartButton.onClick.RemoveAllListeners();
      restartButton.onClick.AddListener(loop.RestartGame);
    }

    // Screen の RawImage（カメラ映像）だけを非表示にする。
    // ポーズ描画は別オブジェクトなので残る。
    private static void ApplyCameraImageVisibility()
    {
      var screen = Object.FindFirstObjectByType<Mediapipe.Unity.Screen>();
      if (screen == null)
      {
        return;
      }

      var rawImage = screen.GetComponentInChildren<RawImage>(true);
      if (rawImage != null)
      {
        rawImage.enabled = ShowCameraImage;
      }
    }

    private static GameObject[] CreateOrFindPlayers()
    {
      var players = new GameObject[MaxPlayers];
      for (var i = 0; i < MaxPlayers; i++)
      {
        var playerName = GetPlayerObjectName(i);
        var existing = GameObject.Find(playerName);
        if (existing == null && i == 0)
        {
          existing = GameObject.Find(LegacyPlayerObjectName);
        }

        if (existing == null)
        {
          existing = new GameObject(playerName);
        }

        EnsurePlayerComponents(existing, i);
        players[i] = existing;
      }

      return players;
    }

    private static GameObject CreateOrFindEnemyTemplate()
    {
      var existing = GameObject.Find(EnemyTemplateName);
      if (existing != null)
      {
        EnsureEnemyComponents(existing);
        return existing;
      }

      var enemy = new GameObject(EnemyTemplateName);
      enemy.transform.position = new Vector3(0f, 10f, 0f);
      enemy.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
      EnsureQuadVisual(enemy, new Color(1f, 0.3f, 0.3f, 1f), isPlayer: false);

      var boxCollider = EnsureComponent<BoxCollider2D>(enemy);
      if (boxCollider != null)
      {
        boxCollider.isTrigger = true;
      }

      var rb = EnsureComponent<Rigidbody2D>(enemy);
      if (rb != null)
      {
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
      }
      else
      {
        Debug.LogError("BodyShooter: Failed to attach Rigidbody2D to EnemyTemplate.");
      }

      _ = enemy.GetComponent<EnemyMover2D>() ?? enemy.AddComponent<EnemyMover2D>();
      _ = enemy.GetComponent<DamageOnContact>() ?? enemy.AddComponent<DamageOnContact>();
      enemy.SetActive(false);
      return enemy;
    }

    private static void EnsurePlayerComponents(GameObject player, int playerIndex)
    {
      player.name = GetPlayerObjectName(playerIndex);
      player.layer = 0;
      player.transform.position = PlayerPositions[playerIndex];
      player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
      player.transform.rotation = Quaternion.identity;

      EnsureQuadVisual(player, PlayerColors[playerIndex], isPlayer: true);

      var box = EnsureComponent<BoxCollider2D>(player);
      if (box != null)
      {
        box.isTrigger = true;
      }

      var rb = EnsureComponent<Rigidbody2D>(player);
      if (rb != null)
      {
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
      }
      else
      {
        Debug.LogError($"BodyShooter: Failed to attach Rigidbody2D to {player.name}.");
      }

      var ship = EnsureComponent<PlayerShipController2D>(player);
      if (ship != null)
      {
        ship.ConfigureBounds(PlayerLaneBounds[playerIndex].x, PlayerLaneBounds[playerIndex].y, -4.5f, 4.5f);
      }

      _ = EnsureComponent<PlayerHealth>(player);
    }

    private static void EnsureEnemyComponents(GameObject enemy)
    {
      enemy.name = EnemyTemplateName;
      enemy.layer = 0;
      enemy.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
      enemy.transform.rotation = Quaternion.identity;
      var p = enemy.transform.position;
      p.z = 0f;
      enemy.transform.position = p;

      EnsureQuadVisual(enemy, new Color(1f, 0.3f, 0.3f, 1f), isPlayer: false);

      var box = EnsureComponent<BoxCollider2D>(enemy);
      if (box != null)
      {
        box.isTrigger = true;
      }

      var rb = EnsureComponent<Rigidbody2D>(enemy);
      if (rb != null)
      {
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
      }

      _ = EnsureComponent<EnemyMover2D>(enemy);
      _ = EnsureComponent<DamageOnContact>(enemy);
    }

    private static void EnsureQuadVisual(GameObject target, Color color, bool isPlayer)
    {
      var meshFilter = EnsureComponent<MeshFilter>(target);
      var meshRenderer = EnsureComponent<MeshRenderer>(target);
      if (meshFilter == null || meshRenderer == null)
      {
        Debug.LogError($"BodyShooter: Failed to build quad visual for {target.name}");
        return;
      }

      meshFilter.sharedMesh = GetFallbackQuadMesh();

      Material material;
      if (isPlayer)
      {
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        material = shader != null ? new Material(shader) : null;
      }
      else
      {
        material = _enemyMaterial;
        if (material == null)
        {
          var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
          material = shader != null ? new Material(shader) : null;
          _enemyMaterial = material;
        }
      }

      if (material == null)
      {
        Debug.LogError($"BodyShooter: Shader not found for {target.name}");
        return;
      }

      material.color = color;
      material.renderQueue = isPlayer ? 5000 : 4000;
      meshRenderer.sharedMaterial = material;
      meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      meshRenderer.receiveShadows = false;
      meshRenderer.enabled = true;
      meshRenderer.sortingOrder = isPlayer ? 32767 : 1000;
    }

    private static Mesh GetFallbackQuadMesh()
    {
      if (_fallbackQuadMesh != null)
      {
        return _fallbackQuadMesh;
      }

      var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
      var meshFilter = temp.GetComponent<MeshFilter>();
      _fallbackQuadMesh = meshFilter != null ? meshFilter.sharedMesh : null;
      Object.Destroy(temp);
      return _fallbackQuadMesh;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
      if (go == null)
      {
        return null;
      }

      if (go.TryGetComponent<T>(out var component) && component != null)
      {
        return component;
      }

      return go.AddComponent<T>();
    }

    private static void EnsureMainCameraForGameplayAndUi()
    {
      var cam = Camera.main;
      if (cam == null)
      {
        var camObj = new GameObject("Main Camera");
        cam = camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
      }

      cam.orthographic = true;
      cam.orthographicSize = 5f;
      cam.clearFlags = CameraClearFlags.SolidColor;
      cam.cullingMask = ~0;
      cam.depth = 0f;
      cam.nearClipPlane = 0.3f;
      cam.farClipPlane = 1000f;
      cam.enabled = true;
      cam.transform.position = new Vector3(0f, 0f, -10f);
      cam.transform.rotation = Quaternion.identity;

      var gameplayCamGo = GameObject.Find(GameplayCameraName);
      if (gameplayCamGo != null && gameplayCamGo != cam.gameObject)
      {
        var gameplayCam = gameplayCamGo.GetComponent<Camera>();
        if (gameplayCam != null)
        {
          gameplayCam.enabled = false;
        }
      }
    }

    // デバッグ時のみゾーン境界の縦線を表示する。
    // Debug.isDebugBuild はエディタ実行および Development Build で true、
    // 製品ビルドでは false になるため、本番では自動的に非表示になる。
    private static void EnsureLaneDividers()
    {
      var show = Debug.isDebugBuild;
      var existing = GameObject.Find(LaneDividerObjectName);

      if (!show)
      {
        if (existing != null)
        {
          existing.SetActive(false);
        }
        return;
      }

      var go = existing ?? new GameObject(LaneDividerObjectName);
      go.SetActive(true);
      var view = EnsureComponent<LaneDividerDebugView>(go);
      view?.Configure(MaxPlayers, PlayAreaXMin, PlayAreaXMax, PlayAreaYMin, PlayAreaYMax);
    }

    private static void EnsureEventSystem()
    {
      if (Object.FindFirstObjectByType<EventSystem>() != null)
      {
        return;
      }

      var go = new GameObject("EventSystem");
      go.AddComponent<EventSystem>();
      go.AddComponent<StandaloneInputModule>();
    }

    private static void CreateOrFindUi(out Text[] hpTexts, out Text[] trackingTexts, out GameObject gameOverPanel, out Button restartButton)
    {
      var canvasGo = GameObject.Find(HudCanvasName);
      var canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : null;
      if (canvas == null)
      {
        canvasGo = new GameObject(HudCanvasName);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
      }
      else
      {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      }

      var font = GetBuiltinUiFont();
      hpTexts = new Text[MaxPlayers];
      trackingTexts = new Text[MaxPlayers];
      for (var i = 0; i < MaxPlayers; i++)
      {
        hpTexts[i] = FindOrCreateText(canvas.transform, $"HpText{i + 1}", new Vector2(24f, -24f - (i * 54f)), $"P{i + 1} HP: 3", font);
        trackingTexts[i] = FindOrCreateText(canvas.transform, $"TrackingText{i + 1}", new Vector2(190f, -24f - (i * 54f)), $"P{i + 1}: Lost", font);
      }

      var panel = canvas.transform.Find("GameOverPanel");
      if (panel == null)
      {
        var panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        var image = panelObj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.65f);
        var rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        FindOrCreateCenteredText(panelObj.transform, "GameOverText", new Vector2(0f, 60f), "GAME OVER", font, 42);

        var buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(panelObj.transform, false);
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.95f);
        restartButton = buttonObj.AddComponent<Button>();
        var brt = buttonObj.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(220f, 52f);
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -30f);
        FindOrCreateCenteredText(buttonObj.transform, "RestartLabel", Vector2.zero, "Restart", font, 24, Color.black);
      }
      else
      {
        restartButton = panel.GetComponentInChildren<Button>(true);
      }

      gameOverPanel = panel != null ? panel.gameObject : canvas.transform.Find("GameOverPanel").gameObject;
      gameOverPanel.SetActive(false);
    }

    private static Text FindOrCreateText(
      Transform parent,
      string name,
      Vector2 anchoredPosition,
      string content,
      Font font,
      TextAnchor alignment = TextAnchor.MiddleLeft,
      int fontSize = 24,
      Color? color = null)
    {
      var t = parent.Find(name);
      Text text;
      if (t == null)
      {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        text = go.AddComponent<Text>();
      }
      else
      {
        text = t.GetComponent<Text>() ?? t.gameObject.AddComponent<Text>();
      }

      if (font != null)
      {
        text.font = font;
      }
      text.fontSize = fontSize;
      text.alignment = alignment;
      text.color = color ?? Color.white;
      text.text = content;

      var rt = text.GetComponent<RectTransform>();
      rt.sizeDelta = new Vector2(320f, 40f);
      rt.anchorMin = new Vector2(0f, 1f);
      rt.anchorMax = new Vector2(0f, 1f);
      rt.pivot = new Vector2(0f, 1f);
      rt.anchoredPosition = anchoredPosition;
      return text;
    }

    private static Text FindOrCreateCenteredText(
      Transform parent,
      string name,
      Vector2 anchoredPosition,
      string content,
      Font font,
      int fontSize,
      Color? color = null)
    {
      var text = FindOrCreateText(parent, name, anchoredPosition, content, font, TextAnchor.MiddleCenter, fontSize, color);
      var rt = text.GetComponent<RectTransform>();
      rt.anchorMin = new Vector2(0.5f, 0.5f);
      rt.anchorMax = new Vector2(0.5f, 0.5f);
      rt.pivot = new Vector2(0.5f, 0.5f);
      return text;
    }

    private static Font GetBuiltinUiFont()
    {
      try
      {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      }
      catch
      {
        try
        {
          return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch
        {
          var fallback = Object.FindFirstObjectByType<Font>();
          if (fallback != null)
          {
            return fallback;
          }

          Debug.LogError("BodyShooter: Built-in UI font could not be resolved.");
          return null;
        }
      }
    }

    private static string GetPlayerObjectName(int playerIndex)
    {
      return $"{PlayerObjectNamePrefix}{playerIndex + 1}";
    }
  }
}
