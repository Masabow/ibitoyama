# 複数ポーズ検出時の自機位置バグ 検証・修正レポート

## 問題の概要

複数人のポーズを取得したとき、自機の位置がおかしくなる。

---

## 原因分析

### 該当ファイル
`Assets/Scripts/BodyShooter/BodyPoseInputAdapter.cs` — `PushPoseResult()`

### 問題の仕組み

MediaPipe の `PoseLandmarkerResult.poseLandmarks` は検出した人物のリストを返すが、
**リスト内の順序（index 0, 1, 2…）は左→右順を保証しない**。

修正前のコードは `playerIndex` をそのまま `poseLandmarks[playerIndex]` のインデックスとして使用していた。

```csharp
// 修正前
for (var playerIndex = 0; playerIndex < MaxPlayers; playerIndex++)
{
    if (!TryGetChestCenter(result, playerIndex, out var chestCenter))
    { ... }
    ...
}
```

`RecalculatePlayerLanes()`（`GameLoopController.cs`）はトラッキング中のプレーヤーを
**左から順にレーン分割**する（playerIndex 0 → 左端レーン）。

| 物理的な位置 | MediaPipe の検出順 | 割り当てレーン | 結果 |
|---|---|---|---|
| 左の人 | `poseLandmarks[1]` (偶然2番目) | 右レーン | **ずれる** |
| 右の人 | `poseLandmarks[0]` (偶然1番目) | 左レーン | **ずれる** |

→ 物理的な立ち位置とレーンが入れ替わり、自機が反対側に表示される。

---

## 修正内容

### 方針

`PushPoseResult()` の中で、検出されたすべての胸座標を収集してから
**画面上の左→右順にソート**し、その順番でプレーヤーに割り当てる。

- `mirrorMode = false` のとき：画像X座標が小さい順（左端→右端）
- `mirrorMode = true`  のとき：画像X座標が大きい順（画像右端 = 画面左端）

ソートは最大4件なので挿入ソートで十分。外部ライブラリ追加なし。

### 修正後コード（`PushPoseResult` 全体）

```csharp
public void PushPoseResult(PoseLandmarkerResult result)
{
    if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
    {
        for (var i = 0; i < MaxPlayers; i++)
        {
            SetTrackingState(i, false);
        }
        return;
    }

    // 有効な胸座標を収集する
    var chests = new Vector2[MaxPlayers];
    var validCount = 0;
    var poseCount = Mathf.Min(result.poseLandmarks.Count, MaxPlayers);
    for (var poseIdx = 0; poseIdx < poseCount; poseIdx++)
    {
        if (TryGetChestCenter(result, poseIdx, out var c))
        {
            chests[validCount++] = c;
        }
    }

    // 画面上の左→右順にソート
    // mirrorMode のときは画像右端が画面左に見えるため降順にする
    for (var i = 1; i < validCount; i++)
    {
        var key = chests[i];
        var j = i - 1;
        while (j >= 0 && (mirrorMode ? chests[j].x < key.x : chests[j].x > key.x))
        {
            chests[j + 1] = chests[j];
            j--;
        }
        chests[j + 1] = key;
    }

    for (var playerIndex = 0; playerIndex < MaxPlayers; playerIndex++)
    {
        if (playerIndex >= validCount)
        {
            SetTrackingState(playerIndex, false);
            continue;
        }

        _lastTrackedAt[playerIndex] = Time.unscaledTime;
        SetTrackingState(playerIndex, true);

        var chestCenter = chests[playerIndex];
        var x = mirrorMode ? 1f - chestCenter.x : chestCenter.x;
        var y = invertY ? 1f - chestCenter.y : chestCenter.y;
        _targetChests[playerIndex] = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }
}
```

---

## 修正前後の動作比較

| シナリオ | 修正前 | 修正後 |
|---|---|---|
| 1人のみ検出 | 正常 | 正常（変化なし） |
| 2人検出・左右が偶然一致 | 正常 | 正常 |
| 2人検出・左右が逆順で返る | **自機が入れ替わる** | 左の人→左レーン ✓ |
| 4人検出・順序がバラバラ | **全員ずれる可能性** | 左から順に正しく割当 ✓ |
| `mirrorMode = true` | 同上（反転を考慮せず） | 鏡像に対応したソート ✓ |

---

## 影響範囲

- 変更ファイル: `BodyPoseInputAdapter.cs` のみ
- `PlayerShipController2D`、`GameLoopController`、`BodyShooterAutoBootstrap` は変更なし
- 1人プレイ時の挙動に影響なし
