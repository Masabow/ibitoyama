# ibitoyama Pose Game PoC Scaffold (Unity Only)

このリポジトリは **Unityのみ** で姿勢連動ゲームのPoCを確認するための最小構成です。

## 方針

- Python は使用しません。
- `unity/PoseUdpReceiver.cs` だけで、ローカルモック姿勢データを生成してジェスチャー判定できます。
- 必要になった場合のみ、将来的に外部入力（UDP等）へ拡張可能な構造を残しています。

## 構成

- `unity/PoseUdpReceiver.cs`  
  Unity 側コンポーネント。ローカルモック入力で「両手上げ」判定を行い、`Barrier ON/OFF` をログ出力。

## クイックスタート（Unityのみ）

1. Unity プロジェクトに `unity/PoseUdpReceiver.cs` を追加
2. 空の GameObject にアタッチ
3. Inspector で以下を設定
   - `useUdpInput` = **OFF**
   - `mockCycleSeconds` = 4.0（任意で調整）
4. Play 実行

コンソールに `Barrier ON/OFF` が周期的に表示されれば、姿勢判定ループは動作しています。

## 判定仕様（現在）

- 判定条件: `left_wrist.y < nose.y && right_wrist.y < nose.y`
- 信頼度しきい値: `confidenceThreshold`
- 平滑化: `Vector2.Lerp(..., smoothing)`

この土台に、しゃがみ・左右移動・スキル発動などの判定を順次追加できます。
