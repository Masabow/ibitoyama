# BodyShooter2D 人体検知仕様

最終更新: 2026-03-03

## 1. 目的
`BodyShooter2D.unity` における人体検知の実装経路と、ゲーム入力に変換されるまでの仕様を固定化する。

## 2. 構成（シーン + ランタイム生成）
- シーン側（`BodyShooter2D.unity`）
  - `Solution` オブジェクトに `PoseLandmarkerRunner`（MediaPipe サンプル）を配置。
  - `PoseLandmarkerRunner._poseLandmarkerResultAnnotationController` に `PoseLandmarkerResultAnnotationController` が接続済み。
- ランタイム側（`BodyShooterAutoBootstrap`）
  - `BodyPoseInputAdapter` が無ければ `BodyPoseInput` オブジェクトを生成して追加。
  - `PoseLandmarkerRunner` を見つけた場合、同じ GameObject に `PoseResultBridge` を追加（または再利用）し、`BodyPoseInputAdapter` と接続。

## 3. 検知パイプライン
1. `PoseLandmarkerRunner` がカメラフレームを処理し、`PoseLandmarkerResult` を生成。
2. 結果は `PoseLandmarkerResultAnnotationController` の private フィールド `_currentTarget` に保持される。
3. `PoseResultBridge.Update()` がリフレクションで `_currentTarget` を毎フレーム取得し、`BodyPoseInputAdapter.PushPoseResult()` に渡す。
4. `BodyPoseInputAdapter` がランドマークから胸中心を計算し、ゲーム用の正規化座標へ変換して平滑化。
5. `GameLoopController` が `OnChestNormalizedChanged` / `OnTrackingStateChanged` を購読し、`PlayerShipController2D` に反映。

## 4. 人体位置の算出仕様（BodyPoseInputAdapter）
- 使用ランドマーク
  - 左肩: index 11
  - 右肩: index 12
- 胸中心
  - `chest = (leftShoulder + rightShoulder) / 2`
- 座標変換
  - `mirrorMode` が `true` の場合: `x = 1 - x`
  - `invertY` が `true` の場合: `y = 1 - y`
  - 最終的に `Clamp01` で `[0,1]` に制限。
- 平滑化
  - `Mathf.SmoothDamp` を X/Y それぞれに適用。
  - デフォルト `smoothTime = 0.08`。

## 5. トラッキング状態
- `PushPoseResult()` で有効結果が来た時点で `IsTracking = true`。
- 最終検知から `trackingTimeoutSec`（デフォルト 0.25 秒）を超えると `IsTracking = false`。
- `IsTracking` 変化時は `OnTrackingStateChanged(bool)` を発火。

## 6. ゲーム側の反映仕様
- `GameLoopController`
  - `OnChestNormalizedChanged(Vector2)` を受けて `PlayerShipController2D.SetChestNormalized()` を呼ぶ。
  - `OnTrackingStateChanged(bool)` を受けて UI 表示を `Tracking` / `Lost` に切替。
  - 同時に `PlayerShipController2D.SetTrackingState()` へ反映。
- `PlayerShipController2D`
  - `IsTracking == false` の間は移動更新しない。
  - `IsTracking == true` の間、胸の正規化座標を `xMin..xMax`, `yMin..yMax` へ線形変換して絶対配置。
  - 非トラッキング時はプレイヤー色を赤に変更（再検知で通常色へ戻す）。

## 7. 失敗時フォールバック
- `PoseLandmarkerRunner` がシーン内に無い場合
  - `BodyShooterAutoBootstrap` は Warning を出し、Pose 連動は無効。
  - ただし `PlayerShipController2D.keyboardFallback` を有効化すればキーボード入力で操作可能（デフォルトは `false`）。

## 8. 実装上の注意
- `PoseResultBridge` は MediaPipe 側 private フィールド名
  - `PoseLandmarkerRunner._poseLandmarkerResultAnnotationController`
  - `PoseLandmarkerResultAnnotationController._currentTarget`
  に依存している。
- MediaPipe パッケージ更新でフィールド名や型が変わると連携が壊れる可能性があるため、更新時は最優先で動作確認する。

## 9. 関連ファイル
- `ibitoyama/Assets/Scenes/BodyShooter2D.unity`
- `ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/PoseResultBridge.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/BodyPoseInputAdapter.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/GameLoopController.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/PlayerShipController2D.cs`
- `ibitoyama/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkerRunner.cs`
- `ibitoyama/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkerResultAnnotationController.cs`
