using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  public class PlayerShipController2D : MonoBehaviour
  {
    [SerializeField] private float xMin = -7f;
    [SerializeField] private float xMax = 7f;
    [SerializeField] private float yMin = -4.5f;
    [SerializeField] private float yMax = 4.5f;
    [SerializeField] private bool keyboardFallback = false;

    private Vector2 _chestNormalized = new Vector2(0.5f, 0.5f);
    private bool _trackingAvailable = true;

    private MeshRenderer _meshRenderer;
    private Color _normalColor = new Color(0.2f, 0.8f, 1f, 1f);
    private readonly Color _lostColor = new Color(1f, 0.2f, 0.2f, 1f);

    private void Awake()
    {
      _meshRenderer = GetComponent<MeshRenderer>();
      if (_meshRenderer != null && _meshRenderer.material != null)
      {
        _normalColor = _meshRenderer.material.color;
      }
    }

    public void SetChestNormalized(Vector2 normalized)
    {
      _chestNormalized = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
    }

    public void SetTrackingState(bool isTracking)
    {
      _trackingAvailable = isTracking;
      ApplyTrackingColor();
    }

    private void Update()
    {
      if (!_trackingAvailable)
      {
        return;
      }

      var target = new Vector3(
        Mathf.Lerp(xMin, xMax, _chestNormalized.x),
        Mathf.Lerp(yMin, yMax, _chestNormalized.y),
        transform.position.z);

      if (keyboardFallback)
      {
        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f)
        {
          target.x = Mathf.Clamp(transform.position.x + h * 6f * Time.deltaTime, xMin, xMax);
          target.y = Mathf.Clamp(transform.position.y + v * 6f * Time.deltaTime, yMin, yMax);
        }
      }

      transform.position = target;
    }

    private void ApplyTrackingColor()
    {
      if (_meshRenderer == null)
      {
        _meshRenderer = GetComponent<MeshRenderer>();
      }

      if (_meshRenderer == null || _meshRenderer.material == null)
      {
        return;
      }

      _meshRenderer.material.color = _trackingAvailable ? _normalColor : _lostColor;
    }
  }
}
