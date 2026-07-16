# ADR-0009: Lambda 関数のランタイムを Go に変更する

## ステータス

承認済み（ADR-0005・ADR-0006 のランタイム選択部分を置き換える）

## コンテキスト

ADR-0005（レシートOCR Lambda）および ADR-0006（月次レポート Lambda）では、
実装言語として Python 3.12 を選択していた（Boto3 SDK の事例が充実しているという理由）。
ADR-0003（サブスクスケジューリング Lambda）ではランタイムが未明記だった。

Lambda はすべて短期のバッチ処理であり、リクエスト駆動ではないため、
コールドスタートの速さが実運用上の体験に直結する。

## 決定

3つの Lambda 関数（subscription-scheduler, receipt-ocr, monthly-report）すべてを **Go** で実装する。

ビルド: `GOOS=linux GOARCH=arm64 go build -o bootstrap main.go`（ARM64 は Lambda の安価なオプション）
配置: `lambda/` ディレクトリをリポジトリルートに配置し、`backend/` と並列に管理する。

## 検討した代替案

| 案 | メリット | デメリット | 不採用の理由 |
|---|---|---|---|
| 採用案: Go | 単一バイナリで依存なし。コールドスタートが速い。AWS SDK for Go v2 が Textract/SES/S3 に対応済み | Python よりも事例の記事数がやや少ない | — |
| Python 3.12 (変更前) | Boto3 の事例が充実 | インタープリタ起動・依存パッケージ読み込みでコールドスタートが遅い | バッチ処理主体のため起動速度を優先する |
| .NET 8 | バックエンドと言語統一 | Lambda のコールドスタートが最長クラス。JIT 起動コストが高い | Lambda の用途に不向き |

## 結果・影響

- ADR-0005・ADR-0006 のランタイム選択部分をこの ADR で上書きする（アーキテクチャの決定そのものは維持）
- ADR-0003 のランタイム未明記部分もこの ADR で補完する
- `lambda/` ディレクトリ構成（予定）:
  ```
  lambda/
  ├── subscription-scheduler/
  │   └── main.go
  ├── receipt-ocr/
  │   └── main.go
  └── monthly-report/
      └── main.go
  ```
- CI/CD に Go のビルドステップ（`GOOS=linux GOARCH=arm64 go build`）と Lambda デプロイを追加する必要がある
