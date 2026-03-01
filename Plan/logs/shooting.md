# Shooting Change Log

最終更新: 2026-03-01 15:58:29 +09:00

## 運用ルール
- シューティング関連の変更は今後このファイル `plan/logs/shooting.md` に追記する。
- 1変更ごとに「目的 / 変更内容 / 影響ファイル / 検証結果」を残す。

## 2026-03-01: 事前状態
### 目的
- MediaPipe が利用可能な状態から、体操作シューティングの初期実装に着手する。

### 変更内容
- MediaPipe 手動導入手順を `Plan/MediaPipe/MediaPipeManualImport.md` で整理済み。
- WordPress向けブログ雛形を追加済み。

### 影響ファイル
- `Plan/MediaPipe/MediaPipeManualImport.md`
- `Plan/MediaPipe/MediaPipeManualImport.WordPressMacro.md`

### 検証結果
- Pose Landmark Detection サンプルシーンが存在し、土台として利用可能であることを確認。

## 2026-03-01: 体操作シューティング初期実装（固定画面2D / PC+WebCam）
### 目的
- 「移動 + 敵 + 被弾」の最小プレイアブルを実装する。

### 変更内容
- 新規シーン `BodyShooter2D.unity` を追加（Pose Landmark Detectionベース）。
- 以下スクリプトを追加し、ゲームの土台を実装。
  - `BodyPoseInputAdapter`
    - 腰中心X(landmark 23/24)から移動軸を計算
    - 鏡モード、dead zone、max delta、2秒キャリブレーション、tracking timeout を実装
  - `PoseResultBridge`
    - `PoseLandmarkerRunner` を直接改変せず、結果を `BodyPoseInputAdapter` へ橋渡し
  - `PlayerShipController2D`
    - 左右移動、画面外制限、キーボードフォールバック
  - `EnemySpawner2D` / `EnemyMover2D`
    - 上端スポーン、下方向移動、画面外破棄
  - `PlayerHealth` / `DamageOnContact`
    - 接触ダメージ、無敵時間、死亡イベント
  - `GameLoopController`
    - HP表示、Tracking状態表示、Game Over表示、リスタート
  - `BodyShooterAutoBootstrap`
    - 必要オブジェクト（Player/Enemyテンプレート/UI/EventSystem/Loop）の自動生成・接続

### 影響ファイル
- `ibitoyama/Assets/Scenes/BodyShooter2D.unity`
- `ibitoyama/Assets/Scripts/BodyShooter/BodyPoseInputAdapter.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/PoseResultBridge.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/PlayerShipController2D.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/EnemySpawner2D.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/EnemyMover2D.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/PlayerHealth.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/DamageOnContact.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/GameLoopController.cs`
- `ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs`

### 検証結果
- `dotnet build ibitoyama/Assembly-CSharp.csproj -nologo` 成功（0 errors / 0 warnings）。
- git status 上は未コミットの新規追加として検出。

## 現在の未コミット状態（記録時点）
- `?? ibitoyama/Assets/Scenes/BodyShooter2D.unity`
- `?? ibitoyama/Assets/Scripts/`

## 2026-03-01: NullReferenceException 修正（BodyShooterAutoBootstrap）
### 目的
- CreateOrFindPlayer() 実行時の NullReferenceException を解消し、シーン初期化を安定化する。

### 発生症状
- 例外:
  - NullReferenceException: Object reference not set to an instance of an object
  - 発生箇所: BodyShooterAutoBootstrap.CreateOrFindPlayer()

### 原因
- GameObject.CreatePrimitive(PrimitiveType.Quad) 前提の生成処理が環境差分で不安定になり、初期化時に null を踏む経路があった。

### 変更内容
- Player/Enemy 生成を CreatePrimitive から 
ew GameObject + SpriteRenderer ベースに変更。
- 1x1 白テクスチャからのフォールバックスプライト生成を追加。
- BoxCollider2D / Rigidbody2D / 各スクリプト を GetComponent ?? AddComponent で安全に取得。
- C#文法エラー（?? 単独ステートメント）を _ = ... で修正。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 警告2件は既存 ImageSource.cs の obsolete warning のみ。

記録時刻: 2026-03-01 16:15:20 +09:00

## 2026-03-01: MissingComponentException (Rigidbody2D) 修正
### 目的
- Player に Rigidbody2D が無いケースで初期化が落ちる問題を解消する。

### 発生症状
- 例外:
  - MissingComponentException: There is no 'Rigidbody2D' attached to the "Player" game object...
  - 発生箇所: BodyShooterAutoBootstrap.CreateOrFindPlayer()

### 変更内容
- BodyShooterAutoBootstrap に EnsureComponent<T>() を追加。
- CreateOrFindPlayer() を修正し、Player 既存時も必須コンポーネントを補完するよう変更。
- CreateOrFindEnemyTemplate() も同様に既存オブジェクト補完ロジックを追加。
- Rigidbody2D 取得後は null 防御を入れ、失敗時は例外停止ではなく Debug.LogError にフォールバック。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）以外の新規警告なし。

記録時刻: 2026-03-01 16:18:55 +09:00

## 2026-03-01: Built-in Font 例外修正（Arial.ttf -> LegacyRuntime.ttf）
### 目的
- Unity 6 環境での UI 初期化時フォント例外を解消する。

### 発生症状
- 例外:
  - ArgumentException: Arial.ttf is no longer a valid built in font. Please use LegacyRuntime.ttf
  - 発生箇所: BodyShooterAutoBootstrap.CreateOrFindUi()

### 変更内容
- 組み込みフォント取得を Arial.ttf 固定から GetBuiltinUiFont() へ変更。
- GetBuiltinUiFont() は以下順で解決:
  1. LegacyRuntime.ttf
  2. Arial.ttf（後方互換）
  3. シーン内既存 Font を探索
- Text.font 代入を null ガード。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）以外の新規警告なし。

記録時刻: 2026-03-01 16:20:11 +09:00

## 2026-03-01: 自機/敵が見えない問題の修正
### 目的
- ゲーム起動時に「敵が見えない / 自機が見えない」状態を解消する。

### 原因
- 敵: EnemyTemplate を非アクティブで保持しており、Instantiate 後の個体も非アクティブのままだった。
- 自機: Player 名称の既存オブジェクト衝突により、意図しないオブジェクトを流用する可能性があった。

### 変更内容
- EnemySpawner2D.SpawnEnemy() で enemy.SetActive(true) を追加。
- 自機/敵テンプレートの名前を専用化:
  - BodyShooterPlayer
  - BodyShooterEnemyTemplate
- BodyShooterAutoBootstrap の再利用ロジックを改善:
  - まず PlayerShipController2D コンポーネントで既存自機を探索
  - なければ専用名オブジェクトを探索
- EnsurePlayerComponents / EnsureEnemyComponents で位置・回転・スケール・SpriteRenderer.sortingOrder を強制再設定。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs
- ibitoyama/Assets/Scripts/BodyShooter/EnemySpawner2D.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 16:25:34 +09:00

## 2026-03-01: ゲーム専用カメラを追加（最前面描画）
### 目的
- 自機/敵オブジェクトが表示されない問題に対して、ゲーム描画を専用カメラで最前面に重ねる。

### 変更内容
- BodyShooterAutoBootstrap に BodyShooter Camera を追加する EnsureGameplayCamera() を実装。
- 専用カメラ設定:
  - orthographic = true
  - orthographicSize = 5
  - clearFlags = Depth
  - depth = 100（Main Cameraより後に描画）
  - cullingMask = Everything
- SetupScene() で EnsureMainCameraOrthographic() 後に EnsureGameplayCamera() を呼び出し。
- 自機/敵の layer を Default(0) に固定、z=0 を明示。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 16:37:41 +09:00

## 2026-03-01: 自機/敵の見た目をメッシュ方式へ変更
### 目的
- SpriteRenderer 依存で表示されないケースを避け、確実に見える形にする。

### 変更内容
- 自機/敵のビジュアルを SpriteRenderer から MeshRenderer + MeshFilter に変更。
- 1回だけ PrimitiveType.Quad からメッシュを取得してキャッシュし、各オブジェクトへ適用。
- マテリアルは Sprites/Default（なければ Unlit/Color）で生成し、
  - 自機: 水色
  - 敵: 赤
  を設定。
- 既存オブジェクト再利用時も EnsureQuadVisual() で強制補完。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 16:45:51 +09:00

## 2026-03-01: Pose連動しない問題の修正（イベント購読タイミング）
### 目的
- 自機がPose入力で動かない問題を解消する。

### 原因
- GameLoopController が動的生成される際、OnEnable() が Configure() より先に呼ばれ、
  BodyPoseInputAdapter のイベント購読が行われないケースが発生していた。

### 変更内容
- GameLoopController にイベント再バインド処理を追加。
  - Configure() で RebindEvents() を実行
  - OnEnable() でも RebindEvents() を実行
  - OnDisable() で UnbindEvents()
- 二重購読防止のため _eventsBound フラグを追加。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/GameLoopController.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 16:49:01 +09:00

## 2026-03-01: 自機を胸位置追従へ変更 + 未検出時は赤色/停止
### 目的
- 自機を胸の位置に連動させる。
- Pose未検出時は自機を赤色表示にし、移動を停止する。

### 変更内容
- BodyPoseInputAdapter
  - 追従基準を腰(23/24)から胸中心（左右肩 11/12 の中点）に変更。
  - 出力イベントを OnChestXNormalizedChanged(float) に変更。
  - 胸の正規化X（0..1）をスムージングして送出。
- PlayerShipController2D
  - 入力を軸移動から「胸X正規化値の絶対位置配置」に変更。
  - 未検出時(SetTrackingState(false))は移動停止。
  - 未検出時は色を赤、検出時は通常色に復帰。
- GameLoopController
  - BodyPoseInputAdapter の新イベントに購読先を変更。
  - OnChestXNormalizedChanged で自機位置更新。
  - ゲームオーバー時は SetTrackingState(false) で停止状態に統一。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyPoseInputAdapter.cs
- ibitoyama/Assets/Scripts/BodyShooter/PlayerShipController2D.cs
- ibitoyama/Assets/Scripts/BodyShooter/GameLoopController.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 16:57:06 +09:00

## 2026-03-01: X反転修正 + Y軸追従追加
### 目的
- X軸が逆方向に動く問題を修正する。
- 自機を胸位置のY軸にも追従させる。

### 変更内容
- BodyPoseInputAdapter
  - 出力を Vector2（胸の正規化座標）へ拡張。
  - イベントを OnChestNormalizedChanged(Vector2) に変更。
  - mirrorMode の初期値を alse に変更（X反転修正）。
  - invertY=true を追加し、上方向移動をゲーム座標の上へ一致させる。
- PlayerShipController2D
  - SetChestNormalized(Vector2) を追加。
  - xMin/xMax に加え yMin/yMax でY軸も追従。
  - 未検出時は従来どおり赤色・停止。
- GameLoopController
  - 新イベント OnChestNormalizedChanged に接続。
  - Start() で初期位置も反映。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyPoseInputAdapter.cs
- ibitoyama/Assets/Scripts/BodyShooter/PlayerShipController2D.cs
- ibitoyama/Assets/Scripts/BodyShooter/GameLoopController.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 17:06:15 +09:00

## 2026-03-01: 自機をAnnotationより前面に描画
### 目的
- Annotation Layer が前面に来て自機が隠れる問題を解消する。

### 変更内容
- BodyShooterAutoBootstrap.EnsureGameplayCamera()
  - ゲーム専用カメラの depth を 100 -> 1000 に引き上げ。
- BodyShooterAutoBootstrap.EnsureQuadVisual()
  - シェーダー優先順位を Unlit/Color 優先へ変更。
  - 描画キューを明示:
    - 自機: enderQueue = 5000
    - 敵: enderQueue = 4000
  - MeshRenderer.sortingOrder を明示:
    - 自機: 32767
    - 敵: 1000

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 17:17:13 +09:00

## 2026-03-01: ゲーム用カメラのパンチ抜き対応（Main Cameraを背面表示）
### 目的
- ゲーム用カメラで何も描画していない部分を透過的に扱い、Main Cameraの描画を見せる。

### 変更内容
- BodyShooterAutoBootstrap に GameplayLayer = 8 を追加。
- 自機/敵テンプレートの layer を GameplayLayer に変更。
- BodyShooter Camera の設定を以下に固定:
  - clearFlags = Depth（色はクリアしない）
  - cullingMask = 1 << GameplayLayer（ゲーム要素のみ描画）
  - depth = 1000（Main Cameraの後に重ね描画）

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 17:46:47 +09:00

## 2026-03-01: パンチ抜きレイヤー分離のロールバック
### 目的
- 期待どおりに動作しなかったため、ゲーム用カメラのパンチ抜き用設定を取り消す。

### 変更内容
- GameplayLayer=8 の導入を取り消し。
- 自機/敵のレイヤーを Default(0) に戻し。
- BodyShooter Camera の cullingMask を Everything(~0) に戻し。

### 影響ファイル
- ibitoyama/Assets/Scripts/BodyShooter/BodyShooterAutoBootstrap.cs

### 検証結果
- dotnet build ibitoyama/Assembly-CSharp.csproj -nologo 成功（0 errors）。
- 既存 warning（ImageSource.cs の obsolete）のみ。

記録時刻: 2026-03-01 18:04:04 +09:00
