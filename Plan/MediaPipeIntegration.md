# MediaPipe 連携（homuler/MediaPipeUnityPlugin）

## 目的
`PoseUdpReceiver` の入力ソースを MediaPipe Pose 出力に切り替えます。

## 1. プラグインの導入（手動）
MediaPipe はネイティブライブラリとモデルが必要なため、Unity Editor で公式パッケージを導入します。

1. 以下のリリースページからアセットを取得します。
- https://github.com/homuler/MediaPipeUnityPlugin/releases

2. 導入方法は次のどちらかです。
- 方法A（推奨）: `com.github.homuler.mediapipe-*.tgz` を Package Manager から追加
- 方法B: `MediaPipeUnityPlugin-all.zip` を展開して、必要フォルダをプロジェクトへコピー

## 1-B. `MediaPipeUnityPlugin-all.zip` の具体手順
`all.zip` は Unity にそのまま「Import Package...」するファイルではありません。

1. `MediaPipeUnityPlugin-all.zip` を任意の場所に展開します。
2. 展開先にある `Assets/` と `Packages/`（必要なら `ProjectSettings/`）を確認します。
3. あなたのプロジェクト（このリポジトリ）の同名フォルダへ内容をコピーします。
4. Unity Editor に戻り、コンパイル完了まで待ちます。
5. `Window > Package Manager` で MediaPipe パッケージが認識されていることを確認します。

注意:
- `ProjectSettings/` の丸ごと上書きは既存設定を壊す可能性があるため非推奨です。
- 基本は `Assets/` と `Packages/` のみ取り込み、必要差分だけを手動で反映してください。

## 2. Scripting Define Symbol の追加
1. `Project Settings > Player > Other Settings > Scripting Define Symbols` を開きます。
2. `MEDIAPIPE_UNITY_PLUGIN` を追加します。

## 3. シーン配線
1. `PoseUdpReceiver` コンポーネントは残します。
2. `inputMode = MediaPipeExternal` に設定します。
3. 任意の GameObject に `MediaPipePoseBridge` を追加します。
4. `MediaPipePoseBridge.target` に `PoseUdpReceiver` を割り当てます。
5. MediaPipe ランナーのコールバックで次を呼びます。
- `MediaPipePoseBridge.PushResult(result)`

## 4. ランドマーク対応
`MediaPipePoseBridge` は BlazePose の次の index を前提にしています。
- 鼻: `0`
- 左手首: `15`
- 右手首: `16`

## 5. このプロジェクトでの挙動
- カメラプレビューと顔/体ボックス描画は `PoseUdpReceiver` 側で継続します。
- `MediaPipeExternal` 時の姿勢入力は `ApplyExternalPose(...)` からのみ更新されます。
- 1秒以上姿勢が来ない場合は警告ログを出します。

## トラブルシュート
- MediaPipe 型でコンパイルエラーが出る:
- プラグイン導入と `MEDIAPIPE_UNITY_PLUGIN` 定義を確認してください。
- 姿勢が更新されない:
- ランナーが毎フレーム `PushResult(result)` を呼んでいるか確認してください。
- 手上げ判定が逆になる:
- ランナー側の正規化 `y` 方向を確認し、必要なら反転してください。
