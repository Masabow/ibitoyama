# ibitoyama Pose Game PoC Scaffold (Unity Only)

このリポジトリは **Unityのみ** で姿勢連動ゲームのPoCを確認するための最小構成です。

## 方針

- Python は使用しません。
- ローカルモック姿勢データを生成し、ジェスチャー判定まで Unity 内で完結します。
- 将来的な拡張に備えて、Unity標準のプロジェクト構成（`Assets` / `Packages` / `ProjectSettings`）を採用します。

## フォルダ構成（推奨）

```text
Assets/
  Scenes/
  Scripts/
    Runtime/
      Input/
        PoseUdpReceiver.cs
  Prefabs/
  Art/
  Audio/
Packages/
  manifest.json
ProjectSettings/
  ProjectVersion.txt
```

## 各フォルダの役割

- `Assets/`  
  Unityで実際に扱うアセット本体（Scene・Script・Prefab・素材類）を置く場所。
- `Assets/Scenes/`  
  ゲームシーンを配置する場所。まずは `Main.unity` のような1シーン運用から開始。
- `Assets/Scripts/Runtime/Input/`  
  実行時ロジックの入力系スクリプトを配置。`PoseUdpReceiver.cs` はここに置く。
- `Assets/Prefabs/`  
  再利用するゲームオブジェクト雛形を配置。
- `Assets/Art/`, `Assets/Audio/`  
  視覚・音声アセットを機能別に分離して配置。
- `Packages/manifest.json`  
  Unity Package Manager の依存定義。
- `ProjectSettings/ProjectVersion.txt`  
  Unityエディタバージョン管理やプロジェクト設定の基点。

## 現在の実装

- `Assets/Scripts/Runtime/Input/PoseUdpReceiver.cs`  
  ローカルモック入力で「両手上げ」判定を行い、`Barrier ON/OFF` をログ出力します。

## クイックスタート（Unityのみ）

1. Unity Hub からこのフォルダを開く
2. 空の GameObject を作成
3. `PoseUdpReceiver` をアタッチ
4. Inspector で以下を設定
   - `useUdpInput` = **OFF**
   - `mockCycleSeconds` = 4.0（任意で調整）
5. Play 実行

コンソールに `Barrier ON/OFF` が周期的に表示されれば、姿勢判定ループは動作しています。

## 判定仕様（現在）

- 判定条件: `left_wrist.y < nose.y && right_wrist.y < nose.y`
- 信頼度しきい値: `confidenceThreshold`
- 平滑化: `Vector2.Lerp(..., smoothing)`

この土台に、しゃがみ・左右移動・スキル発動などの判定を順次追加できます。

## Planning Docs
- [Camera preview setup](Plan/CameraPreviewSetup.md)
- [MediaPipe integration](Plan/MediaPipeIntegration.md)
