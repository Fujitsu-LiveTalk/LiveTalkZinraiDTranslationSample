# LiveTalkZinraiDTranslationSample
LiveTalk常時ファイル出力で出力したテキストを、Zinrai 文書翻訳で翻訳し、その結果をLiveTalk画面に表示するサンプルです。  
LiveTalkが実装しているリアルタイム翻訳（弊社特許）ではなく、一般的な逐次翻訳（発話が終わってからの翻訳）になります。
本サンプルコードは、.NET Core 3.0で作成しています。コードレベルでは.NET Framework 4.6と互換性があります。

![Process](https://github.com/FujitsuSSL-LiveTalk/LiveTalkZinraiDTranslationSample/blob/images/README.png)

# サンプルコードの動き
本サンプルでは、日本語発話を英語翻訳表示します。サンプルコード動作を簡単に説明すると次のような動作をします。  
1. LiveTalkで音声認識した結果がファイルに出力(LiveTalkでデスクトップ上にLiveTalkOutput.csvファイルとして出力するように常時ファイル出力先を指定)されるので、それを自動的に読込み、Zirani 文書翻訳 APIを呼び出します。
2. Zirani 文書翻訳から戻ってきたテキストをLiveTalkが常時ファイル入力(LiveTalkでデスクトップ上にZinrai.txtファイルとして常時ファイル入力元を指定)として監視しているファイルに出力します。
3. LiveTalkが常時ファイル出力に出力された文書翻訳結果を画面に表示します。このときユーザー名は該当ファイル名となります。
※ LiveTalk連携した他のLiveTalk端末からの発話も対象となり、結果もLiveTalk連携しているすべての端末に表示されます。


# 事前準備
1. [Zinrai 文書翻訳 API](https://www.fujitsu.com/jp/solutions/business-technology/ai/ai-zinrai/services/platform/document-translation/index.html)の申込を行い、Zinrai 文書翻訳 APIが有効なclient_idとclient_secretを入手します。
2. client_idとclient_secretをサンプルコードに指定します。
3. インターネットとの接続がPROXY経由の場合、PROXYサーバーや認証情報を設定してください。
4. LiveTalkで、デスクトップに常時ファイル出力を「LiveTalkOutput.csv」、常時ファイル入力を「Zirai.txt」として指定してください。


# 連絡事項
本ソースコードは、LiveTalkの保守サポート範囲に含まれません。  
頂いたissueについては、必ずしも返信できない場合があります。  
LiveTalkそのものに関するご質問は、公式WEBサイトのお問い合わせ窓口からご連絡ください。

