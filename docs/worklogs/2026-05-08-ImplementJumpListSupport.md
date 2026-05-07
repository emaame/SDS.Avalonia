# 2026-05-08 JumpList サポートの実装

## 概要
タスクバーの JumpList からブックマークされたオーディオデバイスへの切り替え、および音量調整用のポップアップウィンドウを開く機能を実装しました。

## 技術的詳細
- **COM Interop の刷新**: `[GeneratedComInterface]` を使用し、`.NET 10` のソース生成 COM 相互運用を採用しました。これにより、NativeAOT 環境でも動作する JumpList 操作を実現しました。
- **JumpList 管理**: `JumpListManager` を通じて、`ICustomDestinationList` を操作し、ブックマークデバイスと「音量調整」タスクを登録する仕組みを構築しました。
- **マルチプロセス連携**: JumpList 項目がクリックされた際、新規インスタンスを起動し、コマンドライン引数 (`--switch-device`, `--volume-popup`) を介して既存のロジックや専用ウィンドウを呼び出す設計としました。
- **音量ポップアップ**: Avalonia を使用して、枠のない最小限の UI を持つ `VolumePopupWindow` を実装しました。フォーカスを失った際に自動で閉じる挙動を実現しています。

## 検証内容
- **ビルド確認**: `dotnet build` が正常に終了することを確認しました。
- **COM マーシャリング**: `ComInterfaceMarshaller` を使用したポインタ操作が正しく動作するように調整しました。
