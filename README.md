# LspClientSample
OmniSharp.Extensions.LanguageServerを使ったLSP Clientのサンプル  
  
本サンプルではLSP Serverにclangdを使っています。  
clandgのインストールはコマンドプロンプトで以下を実行すると簡単です。

```sh
winget install -e --id LLVM.LLVM
```

LspClientSample/Form1.cs中にclangdのパスをハードコーディングしているので、下記のパスを修正 or clangdをパスの場所に置いてください。  

```csharp
private readonly string _clangdPath = @"C:\Program Files\LLVM\bin\clangd.exe";
```

# 使い方
1. ツールを起動する
1. コードディレクトリの「参照」ボタンをクリック
1. フォルダエクスプローラが表示されるので、clangdで解析したいコードが格納されているルートディレクトリを選択
  - *.c, *.hが検索対象
- コードディレクトリ以下にある.c、.hファイルが「ソースコード」コンボボックスに表示されるので、解析したいコードを選択
- 「実行」ボタンをクリック
  - clangdが実行される(コマンドプロンプトが表示されます)
  - 解析結果が、ツールの下のほうにあるテキストボックスに表示されます
  
