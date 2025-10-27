<div align="center">
   <img src="docs/images/logo.png" alt="Caliph Auction Logo" width="200" />
</div>

# Caliph Auction (Backend)

リアルタイム *ペニーオークション型* アプリケーションの **バックエンド API / SignalR Hub / バッチ処理** を提供する .NET 10 (ASP.NET Core) サービスです。

- 言語 / ランタイム: .NET 10 (preview)
- Web フレームワーク: ASP.NET Core Minimal Hosting
- DB: PostgreSQL (EF Core 10 + Npgsql)
- Realtime: SignalR Hub (`/auctionHub`)
- 認証: JWT (対外連携用 ExternalPaymentJwt も別管理)
- 永続化: EF Core + コードファーストマイグレーション
- 非同期処理: HostedService (AutoBid / AuctionTopUp)
- エラーハンドリング: カスタム例外 `CaliphException` + ミドルウェア

## サイト URL

本番サイト: **https://www.caliphauction.com/**  
（デプロイ状況 / メンテナンスにより一時的にアクセスできない場合があります）

| 機能 | 説明 |
|------|------|
| ユーザー管理 | 登録 / ログイン / ロックアウト / パスワードハッシュ + ソルト |
| JWT 認証 | アクセストークン発行・検証 (Issuer/Key/ExpireMinutes 設定) |
| オークション | 時間延長型。最低残り秒数下回る入札で残り時間延長 (設定値参照) |
| 入札 (Bid) | SignalR 経由リアルタイム更新。最高額/残秒数を Hub ブロードキャスト |
| ポイント管理 | 付与 / 消費 / ロット管理 / 取引エントリ (PointTransaction / Entries / Lots) |
| 自動入札 (AutoBid) | HostedService により間隔監視・投入キュー制御 |
| 付与キャンペーン | Registration Bonus など設定駆動 (Points.RegistrationBonus) |
| 通知 | DB 永続化 (Notification) / 将来的なプッシュ配信基盤前提 |
| 失敗ログイン追跡 | IP / ユーザ別失敗回数でロックアウト条件評価 |

## スクリーンショット

![list](docs/images/home.png)

## リポジトリ構成

| 名前                    | リンク                                                      | 役割 / 概要                       |
| ----------------------- | ----------------------------------------------------------- | --------------------------------- |
| Frontend (本リポジトリ) | https://github.com/xm-i/CaliphAuctionFront          | SPA / Vue3 / SignalR クライアント |
| Backend                 | https://github.com/xm-i/CaliphAuctionBackend        | REST API / 入札 BOT / SignalR Hub |
| Infrastructure          | https://github.com/xm-i/CaliphAuctionInfrastructure | IaC / CI/CD / 環境構築スクリプト  |

## プロジェクト構成 (抜粋)

```
CaliphAuctionBackend/
  Data/
    CaliphDbContext.cs            # DbContext + タイムスタンプ管理
    Migrations/ (生成後)          # EF Core マイグレーション
  Middleware/
    CaliphExceptionHandlingMiddleware.cs
  Services/
    Background/                   # HostedService 実装
    Implementations/              # ドメインサービス (属性で自動 DI 登録)
    Infrastructure/               # 設定クラス / オプション
  Hubs/
    AuctionHub.cs                 # SignalR Hub (リアルタイム入札)
  Models/                         # エンティティ
  Utils/                          # セキュリティユーティリティ等
  Program.cs                      # エントリ / DI / パイプライン
appsettings.json
appsettings.Development.json      # 開発時 SQL ログレベル緩和
```

---

## 起動 / 開発

### 1. 依存関係

- .NET 10 SDK (preview)
- PostgreSQL 15+ (ローカルコンテナ可)

例 (Docker で DB):

```bash
docker run -d --name caliph-pg -p 5432:5432 \
  -e POSTGRES_PASSWORD=caliph -e POSTGRES_USER=caliph -e POSTGRES_DB=caliph postgres:15
```

### 2. 環境変数 (最低限)

| 変数 | 目的 |
|------|------|
| ASPNETCORE_ENVIRONMENT | Development / Production |
| ConnectionStrings__DefaultConnection | PostgreSQL 接続文字列 |
| Jwt__Key | JWT 署名キー (十分な長さ) |
| Jwt__Issuer | 発行者名 |
| ExternalPaymentJwt__Key | 外部決済トークン用キー |
| Cors__AllowedOrigins | CORS オリジン (カンマ区切り) |

### 3. 実行

```bash
dotnet restore
dotnet build
dotnet run --project CaliphAuctionBackend/CaliphAuctionBackend.csproj
```

開発用 HTTPS ポートは `https://localhost:5000` (launchSettings で固定)。

---

## マイグレーション

本番サーバーに SDK を置かない方針 → 通常は CI でバイナリ生成 or 手動 SQL。現状ワークフローでは *マイグレーション バンドル* を生成していません (運用: DDL 手動管理)。

ローカルでマイグレーション追加例:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0-preview.7.*
dotnet ef migrations add AddSomething \
  --project CaliphAuctionBackend/CaliphAuctionBackend.csproj \
  --startup-project CaliphAuctionBackend/CaliphAuctionBackend.csproj
# SQL スクリプト出力
dotnet ef migrations script -o diff.sql
```

---

## エラーハンドリング

`CaliphException` をスローすると `StatusCode` に応じた JSON:

```json
{ "error": "<message>", "status": 400 }
```

未処理例外は 500 (Development のみ詳細メッセージ)。

---
## CORS

`Cors:AllowedOrigins` 配列がポリシー `DefaultCorsPolicy` に反映。Credentials 許可 (`AllowCredentials`) のため `*` ワイルドカード不可。GitHub Actions の Production 生成時に環境変数から埋込。

---
## デプロイ (GitHub Actions)
ワークフロー: `.github/workflows/build-publish.yml`
1. Build (Release) → Publish (`/p:UseAppHost=false`)
2. `appsettings.Production.json` を Secrets/Vars で生成
3. アーカイブ `app.tar.gz` アーティファクト化
4. SSH 経由で `DEPLOY_PATH/releases/<timestamp>` へ展開 → `current` シンボリックリンク切り替え
5. systemd ユニット (存在時) 再起動

必要 Secrets:
- `DB_CONNECTION`, `JWT_KEY`, `EXTERNAL_JWT_KEY`, `SSH_KEY`
必要 Envs (Environment Vars):
- `CORS_ALLOWED_ORIGINS`

---
## セキュリティ上の注意
- JWT 秘密鍵は必ず Secrets / Vault 管理
- 自動マイグレーションは本番で無効 (手動 DDL 運用)
- 失敗ログイン記録で IP ブルートフォース軽減
- SQL ログは本番で抑制 (情報漏えいリスク低減)

---
## ライセンス

このリポジトリは「ソースコード閲覧・学習目的での公開」であり、一般的な OSS ライセンス (MIT / Apache など) ではありません。いわゆる _Source-Available_ ポリシーです。

### 許可される行為

- 個人的または社内での学習・参考・評価
- 自身の環境でのビルド・実行・検証
- 一部コード断片 (短い抜粋) を引用した技術記事等への掲載 (出典明記が条件)

### 禁止される行為 (明示的に許可しない)

- 本リポジトリ全体または実質的主要部分の再頒布 (フォークを含む公的再公開)
- コードの改変版を公衆に提供 / ホスティング / SaaS として提供
- ライセンス互換を前提とした他 OSS への組み込み
- 商用目的 (利用・販売・再販) での使用

### 追加注意

- 上記に該当しない利用 (教材化 / セミナー利用 / 研究引用 など) を希望する場合は事前に相談してください。
- いつでもライセンス/公開方針を変更・終了する可能性があります。
- Issue / PR は受け付けますが、マージ/反映は保証されません。

将来的に OSS ライセンスへ移行する場合は明示的に本節を置き換えます。それまでは本記述が優先します。

---
## クイック参照
| 領域 | ファイル |
|------|----------|
| エントリポイント | `Program.cs` |
| 例外ハンドラ | `Middleware/CaliphExceptionHandlingMiddleware.cs` |
| DbContext | `Data/CaliphDbContext.cs` |
| JWT 設定 | `appsettings.json (Jwt.*)` |
| Realtime Hub | `Hubs/AuctionHub.cs` |
| Hosted Services | `Services/Background/*` |
| ドメインサービス例 | `Services/Implementations/UserService.cs` |

---
## 問い合わせ
改善提案 / バグ報告は Issue へ。重大なセキュリティ懸念は公開前に直接連絡してください。
