# ECショップ (FRONTIER)

## プロジェクト概要
このプロジェクトは、ASP.NET Core MVCを使用して開発されたECショッピングアプリケーションです。商品の閲覧、購入、ユーザー認証機能を提供します。

## 機能
- ユーザー認証（登録、ログイン、ログアウト）
- 商品一覧表示
- 商品詳細表示
- 商品購入機能
- 在庫管理
- エラーハンドリング

## 技術スタック
- ASP.NET Core MVC
- Entity Framework Core
- SQLite
- Bootstrap
- C#

## 環境構築

### 前提条件
- .NET 7.0以上
- Visual Studio または Visual Studio Code

### インストール手順
1. リポジトリをクローン
```bash
git clone https://github.com/SunnyDayService321/first_asp_app.git
cd first_asp_app

2.依存関係のインストール

バッシュdotnet restore

3.データベースの初期化

バッシュdotnet ef database update

4.アプリケーションの起動

バッシュdotnet run

## 主な機能詳細

### ユーザー認証

・新規ユーザー登録
・ログイン/ログアウト
・パスワードハッシュ化

### 製品管理

・商品一覧表示
・在庫管理
・論理削除対応

### 注文処理

・商品購入
・在庫数自動更新
・注文履歴

### データベース

・SQLiteを使用
・Entity Framework Coreによるデータアクセス

### セキュリティ

・パスワードのBcrypt暗号化
・Cookie認証
・入力バリデーション

### 今後の改善予定

・カート機能の追加
・注文履歴ページ
・管理者パネル
・決済機能の統合

###このREADME.mdは以下の点に注意して作成しています：

1. プロジェクトの全体像を簡潔に説明
2. 技術スタックを明確に記載
3. 環境構築手順を詳細に説明
4. 主な機能を箇条書きで紹介
5. 今後の改善予定を提示
