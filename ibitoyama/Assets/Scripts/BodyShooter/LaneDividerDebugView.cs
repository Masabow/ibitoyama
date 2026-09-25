using System.Collections.Generic;
using UnityEngine;

namespace Ibitoyama.BodyShooter
{
  // デバッグ用: プレイエリアを横方向に分割するゾーン境界線（縦線）を
  // ゲーム画面（ワールド空間）に表示する。
  // 4人同時プレイ時に、各プレイヤーのレーン境界を目視確認するための補助表示。
  public class LaneDividerDebugView : MonoBehaviour
  {
    [SerializeField] private int laneCount = 4;
    [SerializeField] private float xMin = -7f;
    [SerializeField] private float xMax = 7f;
    [SerializeField] private float yMin = -4.5f;
    [SerializeField] private float yMax = 4.5f;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.35f);

    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private bool _built;

    public void Configure(int lanes, float minX, float maxX, float minY, float maxY)
    {
      laneCount = Mathf.Max(1, lanes);
      xMin = minX;
      xMax = maxX;
      yMin = minY;
      yMax = maxY;
      Rebuild();
    }

    private void Start()
    {
      if (!_built)
      {
        Rebuild();
      }
    }

    private void Rebuild()
    {
      foreach (var line in _lines)
      {
        if (line != null)
        {
          Destroy(line.gameObject);
        }
      }
      _lines.Clear();

      var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
      var dividerCount = laneCount - 1;
      var laneWidth = (xMax - xMin) / laneCount;

      for (var i = 1; i <= dividerCount; i++)
      {
        var x = xMin + (laneWidth * i);
        var go = new GameObject($"LaneDivider{i}");
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(x, yMin, 0f));
        lr.SetPosition(1, new Vector3(x, yMax, 0f));
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCapVertices = 0;
        lr.alignment = LineAlignment.View;
        lr.sortingOrder = 20000;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        if (shader != null)
        {
          lr.material = new Material(shader) { color = lineColor };
        }
        lr.startColor = lineColor;
        lr.endColor = lineColor;

        _lines.Add(lr);
      }

      _built = true;
    }
  }
}
