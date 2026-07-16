# バックエンド コーディング規約

## コーディング規約(.NET)

- Nullable参照型を有効にする(`<Nullable>enable</Nullable>`)
- 非同期メソッドは `Async` サフィックス、`CancellationToken` を受け取る
- 入力バリデーションは FluentValidation を使う
- Controllerには薄いロジックのみ。ビジネスロジックはApplication層のユースケースに書く

## コメント規約

コードの意図が自明でない場合は、以下の観点でコメントを付ける。

- **プロパティ**: 何を格納するためのプロパティなのかを `///` XMLドキュメントコメントで明記する
  ```csharp
  /// <summary>支出が発生した日付</summary>
  public DateOnly Date { get; private set; }
  ```

- **メソッド**: 何をするメソッドなのかを `///` XMLドキュメントコメントで明記する
  ```csharp
  /// <summary>指定月の予算残高を計算して返す</summary>
  ```

- **引数**: 何を渡すパラメーターなのかを `<param>` タグで明記する
  ```csharp
  /// <param name="month">集計対象の年月</param>
  /// <param name="cancellationToken">キャンセルトークン</param>
  ```

- **戻り値**: 何を返すのかを `<returns>` タグで明記する
  ```csharp
  /// <returns>予算残高(円)。予算未設定の場合は null</returns>
  ```

> XMLドキュメントコメントが不要なほど自明なプロパティ・メソッド（例: `Id`, `Name` など）は省略してよい。
> ロジックの複雑な箇所には `//` インラインコメントで補足する。
