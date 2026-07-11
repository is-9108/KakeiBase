# ADR-0003: サブスクのスケジューリング責務を Lambda に委譲する

- ステータス: 承認済み
- 日付: 2026-07-11
- 決定者: @syout

## コンテキスト

サブスク管理機能において、毎月の自動課金から収支レコードを生成する仕組みが必要。
その際、「いつ請求が発生するか」の情報（billing_cycle / billing_day / next_billing_date）を
どこに持たせるかを検討した。

## 検討した選択肢

### A: DBドリブン（Subscription テーブルに billing_cycle 等を持つ）
- Subscription に `billing_cycle`（monthly/yearly 等）・`billing_day`（毎月何日）・`next_billing_date` を追加
- アプリ側（バックエンド or バッチ処理）が next_billing_date を参照して収支レコードを生成

**メリット:**
- スケジューリングロジックがアプリコード内に閉じる
- DB を見れば次回請求日が一目でわかる

**デメリット:**
- next_billing_date の更新ロジックがアプリ側に必要（更新漏れのリスク）
- 月次一括処理で全レコードをスキャンするクエリが重くなりうる
- エンティティが複雑化する（月次固定の v1 スコープで過剰）

### B: Lambda 委譲（Subscription テーブルをシンプルに保つ）← 採用
- Subscription は `id / user_id / category_id / name / amount / is_active` のみ持つ
- 月次 Lambda が全アクティブサブスクを取得して収支レコードを一括生成
- スケジュールは Lambda の EventBridge ルール（毎月 1 日実行など）で管理

**メリット:**
- Subscription エンティティがシンプルで、ドメインロジックが `GenerateTransaction()` に集約される
- billing_cycle の多様化（年次・週次等）が v1 では不要なため、オーバーエンジニアリングを避けられる
- Lambda 側でリトライ・エラー通知などインフラ的関心事を分離できる

**デメリット:**
- スケジューリングロジックが Lambda コードに散る（アプリとインフラの境界をまたぐ）
- Lambda を別途実装・デプロイする必要がある

## 決定

**選択肢 B（Lambda 委譲）を採用する。**

v1 スコープでは月次固定の課金のみを対象とし、billing_cycle の多様化は想定しない。
Subscription テーブルはシンプルに保ち、ドメインロジック（`GenerateTransaction()`）を
エンティティに集約する。スケジューリングは Lambda + EventBridge に委譲する。

## 結果

- `Subscription` エンティティは `billing_cycle / billing_day / next_billing_date` を持たない
- Lambda が毎月 1 日に全アクティブサブスクを取得し、`GenerateTransaction()` に相当する
  収支レコード生成 API を呼び出す（または直接 DB 書き込みを行う）
- 将来的に billing_cycle の多様化が必要になった場合は、このADRを更新し
  選択肢Aへの移行を改めて検討する
