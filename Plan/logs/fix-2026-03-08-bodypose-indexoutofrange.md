# 2026-03-08 BodyPoseInputAdapter 修正メモ

## 事象
- 例外: `ArgumentOutOfRangeException: Index was out of range`
- 発生箇所: `BodyPoseInputAdapter.TryGetChestCenter`（肩ランドマーク取得時）

## 原因
- `PoseLandmarkerResult` 内の `poseLandmarks / landmarks` を読むタイミングで、
  内部リスト更新と競合しインデックスが不正化するケースがあった。

## 対応
- `TryGetChestCenter` で以下を追加:
  - `result.poseLandmarks` の null チェック
  - `poseLandmarks.Count == 0` チェック
  - `pose.landmarks` の null チェック
  - `LeftShoulderIndex / RightShoulderIndex` の境界チェック
  - `ArgumentOutOfRangeException` を捕捉して `false` を返す

## 影響
- 追跡不能フレームは安全にスキップし、クラッシュを回避。

## 確認
- `dotnet build ibitoyama/Assembly-CSharp.csproj -nologo` 成功（error 0）
- 既存の obsolete warning 2件のみ（今回対応外）
