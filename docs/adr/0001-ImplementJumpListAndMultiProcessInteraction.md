# 0001-JumpListAndMultiProcessInteraction
# 0001-JumpListとマルチプロセス連携の実装

JumpListの実装とそれに伴う設計上の決定事項を記録します。

## Metadata
- **Status**: Accepted
- **Date**: 2026-05-08
- **Deciders**: Antigravity (AI Architect)

## Context & Problem Statement
ユーザーがアプリケーションのメインウィンドウを開くことなく、タスクバーの右クリックメニュー（JumpList）から素早くオーディオデバイスを切り替え、または音量を調整したいという要望がありました。WindowsのJumpListは標準でコントロールの埋め込みをサポートしていないため、外部プロセスや追加ウィンドウを介した設計が必要でした。

## Key Architectural Decisions
1. **Source-Generated COM の採用**:
   - 理由: `.NET 8/9/10` における AOT コンパイル対応を考慮し、従来の `ComImport` ではなく `[GeneratedComInterface]` を使用しました。
2. **コマンドライン引数によるアクション実行**:
   - 理由: JumpList の項目は実行ファイルの起動を伴うため、引数 (`--switch-device`, `--volume-popup`) を解析して特定のアクションのみを実行し、即座に終了または専用ウィンドウを表示するパスを `Program.Main` に追加しました。
3. **専用の音量ポップアップウィンドウ**:
   - 理由: JumpList 内にスライダーを置くことができない制約を回避するため、`VolumePopupWindow` という最小限の UI を持つウィンドウを別途起動する方式を採用しました。

## Rejected Alternatives & Trade-offs
- **Rejected: UIオートメーション等による外部操作**: 複雑さと不安定さが増すため却下。
- **Trade-off: インスタンスの多重起動**: JumpList 項目をクリックするたびに新しいプロセスが起動しますが、スイッチ操作であれば即座に終了し、ポップアップであれば軽量なウィンドウのみを表示するため、リソース消費は許容範囲内と判断しました。

## System Architecture & Data Flow
1. ユーザーが JumpList 項目をクリック。
2. Windows が引数付きで `SDS.Avalonia.exe` を起動。
3. `Program.Main` で引数を解析。
4. デバイス切り替えの場合は `AudioController` を呼び出し終了。音量調整の場合は `VolumePopupWindow` を表示。

## Threading Model & State Management
- `VolumePopupWindow` は STA スレッド（Avalonia のメインスレッド）で動作。
- `AudioController` の操作は COM 経由で同期的に実行。

## Class Structure & Component Roles
- `JumpListManager`: JumpList の構築と更新を担当。
- `JumpListInterop`: COM インターフェースの定義と低レイヤーのマーシャリング。
- `VolumePopupWindow`: 音量操作 UI。
- `Program / App`: エントリポイントおよび引数ハンドリング。

## Performance Metrics & Resource Constraints
- **メモリ**: ポップアップウィンドウ起動時のワーキングセット増加は最小限。
- **実行時間**: デバイス切り替え引数時は、UI フレームワークの初期化をスキップして高速に実行。

## Challenges Encountered & Resolution
- **COM Marshaling**: `GeneratedComInterface` において `object` や一部の `MarshalAs` がサポートされていない問題に直面しましたが、`nint` と `ComInterfaceMarshaller` を併用した手動マーシャリングに切り替えることで解決しました。

## Future Work & Unresolved Issues
- 複数インスタンス間での状態同期（メインウィンドウが開いている場合にポップアップでの変更を即座に反映させる IPC 等）は今後の拡張課題。
