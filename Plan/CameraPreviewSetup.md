# カメラプレビュー設定（Unity）

## 現在の動作
- `PoseUdpReceiver` は `OnGUI` でWebカメラ映像を自動表示します。
- 基本的なリアルタイム表示には、Unity GUI の手動設定は不要です。

## Inspector 設定（推奨）
1. `PoseUdpReceiver` を付けた GameObject を選択します。
2. `inputMode = WebcamDirect` にします。
3. `showWebcamPreview = true` にします。
4. 必要に応じて以下を調整します。
- `previewScale`（初期値 `0.33`）
- `previewMargin`（初期値 `(16,16)`）
- `webcamWidth / webcamHeight / webcamFps`

## Unity GUI を手動で組む場合
`OnGUI` ではなく、UGUI で表示位置を細かく制御したい場合の手順です。

1. Hierarchy に `Canvas` を作成します（なければ）。
2. `Canvas` 配下に `RawImage`（名前例: `WebcamPreview`）を作成します。
3. `WebcamPreview` のアンカーとサイズを調整します。
4. `RawImage` にカメラテクスチャを渡す処理を追加します。
- 例: `rawImage.texture = webcamTexture`
5. 必要に応じて `RawImage` の UV Rect や回転を調整します（端末向き対策）。

## トラブルシュート
- 画面が黒い: OS 側で Unity Editor のカメラ権限を確認してください。
- デバイス未検出: `PoseUdpReceiver` は `LocalMock` にフォールバックします。
- 上下反転している: `UV Rect` または UI の回転で補正してください。
