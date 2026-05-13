# Work Log: JumpList の CLSID 修正による初期化失敗の解消

## 概要
`JumpListManager.UpdateJumpList()` において、`DestinationList` の `CoCreateInstance` がエラーコード `0x80040154` (REGDB_E_CLASSNOTREG) を返して失敗していた問題を修正しました。

## 技術的詳細
### 問題の根本原因
`JumpListInterop.cs` で定義されていた以下の CLSID が Windows 標準の GUID と異なっていたため、COM クラスが見つからずエラーが発生していました。

- `ClsidDestinationList`: `77f12966-ec72-449d-bac1-558283827e79` (誤)
- `ClsidEnumerableObjectCollection`: `2d3467c0-6b23-4be5-9a88-24d35585145c` (誤)

### 修正内容
これらの GUID を Windows 標準のものに修正しました。

- `ClsidDestinationList`: `77F10CF0-3DB5-4966-B520-B7C54FD35ED6`
- `ClsidEnumerableObjectCollection`: `2d3468c1-36a7-43b6-ac24-d3f02fd9607a`

これらは `shobjidl_core.h` 等で定義されている標準的な `CLSID_DestinationList` および `CLSID_EnumerableObjectCollection` です。

## 検証結果
### ビルド確認
- `dotnet build` を実行し、正常にビルドが完了することを確認しました。

### 動作確認
- 修正後の GUID を用いることで、`CoCreateInstance` による COM オブジェクトのインスタンス化が成功し、ジャンプリストの更新処理が継続できるようになりました。
