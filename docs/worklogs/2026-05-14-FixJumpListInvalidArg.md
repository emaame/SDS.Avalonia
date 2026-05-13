# Work Log: JumpList の E_INVALIDARG エラーの解消

## 概要
`AddUserTasks()` および `AppendCategory()` が `0x80070057` (E_INVALIDARG) を返していた問題を修正しました。

## 技術的詳細
### 問題の根本原因
1. **インターフェースの継承不足**: `IObjectCollection` が `IObjectArray` を継承していなかったため、COM プロキシのマーシャリング時に適切なインターフェースとして認識されない可能性がありました。
2. **PropertyStore 取得の不確実性**: .NET の `is` 演算子による COM インターフェースの判定が、ソース生成された COM プロキシに対して期待通りに動作せず、`IShellLink` のタイトル（PKEY_Title）が設定されていなかった可能性があります。ジャンプリストの項目にはタイトルが必須であり、不足していると `E_INVALIDARG` が返されます。

### 修正内容
1. **JumpListInterop.cs**:
   - `IObjectCollection` に `: IObjectArray` を追加し、重複するメソッド定義を削除しました。
   - `IidPropertyStore` を追加しました。
2. **JumpListManager.cs**:
   - `CreateShellLink` 内で `Marshal.QueryInterface` を使用して確実に `IPropertyStore` を取得するように変更しました。
   - `AddObject` の戻り値チェックを追加しました。
   - `CreateShellLink` を `unsafe` に変更しました。

## 検証結果
### ビルド確認
- コンパイルエラーが解消され、ビルドが通ることを確認しました。

### 動作期待
- タイトルが確実に設定されるようになり、シェルによる引数の検証を通過するようになります。
