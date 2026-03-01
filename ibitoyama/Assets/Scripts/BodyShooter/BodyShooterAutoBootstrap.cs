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
    private const string PlayerObjectName = "BodyShooterPlayer";
    private const string EnemyTemplateName = "BodyShooterEnemyTemplate";
    private const string GameplayCameraName = "BodyShooter Camera";
    private static bool _initializedForScene;
    private static Mesh _fallbackQuadMesh;
    private static Material _playerMaterial;
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

      var player = CreateOrFindPlayer();
      var enemyTemplate = CreateOrFindEnemyTemplate();
      var spawner = Object.FindFirstObjectByType<EnemySpawner2D>();
      if (spawner == null)
      {
        spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner2D>();
      }
      spawner.Configure(enemyTemplate, -7f, 7f, 5.5f);

      EnsureMainCameraOrthographic();
      EnsureGameplayCamera();
      EnsureEventSystem();
      CreateOrFindUi(out var hpText, out var trackingText, out var gameOverPanel, out var restartButton);

      var loop = Object.FindFirstObjectByType<GameLoopController>();
      if (loop == null)
      {
        loop = new GameObject("GameLoop").AddComponent<GameLoopController>();
      }

      var health = player.GetComponent<PlayerHealth>();
      var ship = player.GetComponent<PlayerShipController2D>();
      loop.Configure(health, ship, spawner, poseInput, hpText, trackingText, gameOverPanel);

      restartButton.onClick.RemoveAllListeners();
      restartButton.onClick.AddListener(loop.RestartGame);
    }

    private static GameObject CreateOrFindPlayer()
    {
      var existingPlayerByComponent = Object.FindFirstObjectByType<PlayerShipController2D>();
      if (existingPlayerByComponent != null)
      {
        EnsurePlayerComponents(existingPlayerByComponent.gameObject);
        return existingPlayerByComponent.gameObject;
      }

      var existingPlayer = GameObject.Find(PlayerObjectName);
      if (existingPlayer != null)
      {
        EnsurePlayerComponents(existingPlayer);
        return existingPlayer;
      }

      var player = new GameObject(PlayerObjectName);
      player.transform.position = new Vector3(0f, -4f, 0f);
      player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
      EnsureQuadVisual(player, new Color(0.2f, 0.8f, 1f, 1f), isPlayer: true);

      var boxCollider = EnsureComponent<BoxCollider2D>(player);
      if (boxCollider != null)
      {
        boxCollider.isTrigger = true;
      }

      var rb = EnsureComponent<Rigidbody2D>(player);
      if (rb != null)
      {
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
      }
      else
      {
        Debug.LogError("BodyShooter: Failed to attach Rigidbody2D to Player.");
      }

      _ = player.GetComponent<PlayerShipController2D>() ?? player.AddComponent<PlayerShipController2D>();
      _ = player.GetComponent<PlayerHealth>() ?? player.AddComponent<PlayerHealth>();
      return player;
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

    private static void EnsurePlayerComponents(GameObject player)
    {
      player.name = PlayerObjectName;
      player.layer = 0;
      player.transform.position = new Vector3(0f, -4f, 0f);
      player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
      player.transform.rotation = Quaternion.identity;
      var p = player.transform.position;
      p.z = 0f;
      player.transform.position = p;

      EnsureQuadVisual(player, new Color(0.2f, 0.8f, 1f, 1f), isPlayer: true);

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

      _ = EnsureComponent<PlayerShipController2D>(player);
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

      var material = isPlayer ? _playerMaterial : _enemyMaterial;
      if (material == null)
      {
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        material = shader != null ? new Material(shader) : null;
        if (material == null)
        {
          Debug.LogError($"BodyShooter: Shader not found for {target.name}");
          return;
        }

        material.color = color;
        if (isPlayer)
        {
          _playerMaterial = material;
        }
        else
        {
          _enemyMaterial = material;
        }
      }
      else
      {
        material.color = color;
      }

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

    private static void EnsureMainCameraOrthographic()
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
      cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void EnsureGameplayCamera()
    {
      var camGo = GameObject.Find(GameplayCameraName);
      Camera cam;
      if (camGo == null)
      {
        camGo = new GameObject(GameplayCameraName);
        cam = camGo.AddComponent<Camera>();
      }
      else
      {
        cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
      }

      cam.orthographic = true;
      cam.orthographicSize = 5f;
      cam.clearFlags = CameraClearFlags.Depth;
      cam.cullingMask = ~0;
      cam.depth = 1000f;
      cam.nearClipPlane = 0.3f;
      cam.farClipPlane = 1000f;
      cam.enabled = true;

      cam.transform.position = new Vector3(0f, 0f, -10f);
      cam.transform.rotation = Quaternion.identity;
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

    private static void CreateOrFindUi(out Text hpText, out Text trackingText, out GameObject gameOverPanel, out Button restartButton)
    {
      var canvas = Object.FindFirstObjectByType<Canvas>();
      if (canvas == null)
      {
        var canvasGo = new GameObject("Canvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
      }

      var font = GetBuiltinUiFont();
      hpText = FindOrCreateText(canvas.transform, "HpText", new Vector2(100f, -24f), "HP: 3", font);
      trackingText = FindOrCreateText(canvas.transform, "TrackingText", new Vector2(100f, -54f), "Lost", font);

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

        FindOrCreateText(panelObj.transform, "GameOverText", new Vector2(0f, 60f), "GAME OVER", font, TextAnchor.MiddleCenter, 42);

        var buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(panelObj.transform, false);
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.95f);
        restartButton = buttonObj.AddComponent<Button>();
        var brt = buttonObj.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(220f, 52f);
        brt.anchoredPosition = new Vector2(0f, -30f);
        FindOrCreateText(buttonObj.transform, "RestartLabel", Vector2.zero, "Restart", font, TextAnchor.MiddleCenter, 24, Color.black);
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
  }
}
