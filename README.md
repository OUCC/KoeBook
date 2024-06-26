# KoeBook

[青空文庫](https://www.aozora.gr.jp/)や[小説家になろう](https://syosetu.com/)にある小説の読み上げ音声をAIによって生成し、EPUBとして出力します。
AIを用いて話者を特定して適切な音声を生成するので場面にあった音声を生成できます。

[LINEヤフー株式会社様より企業賞を受賞しました。](https://kc3.me/news/2405/)


## 目的

- どんな本でも高品質の音声で聞けるようにする
- 家事や運転中でも、ラジオのように聞けるようにする
- 視覚障害者がどんな本でもアクセスできるようにする


## 操作説明

https://github.com/kc3hack/2024_H/assets/84168445/2c265fee-792e-4089-93cb-8ddfa401cb0d

（注）動画はイメージです。

1. 最初の画面で音声朗読させたい青空文庫か小説家になろうの作品のリンクを張る
   - 青空文庫は図書カードページのxhtmlファイルのリンク
   - 小説家になろうは目次のページ
2. GPTの解析が終わるまで放置
3. （やりたい人は）セリフに対応するキャラクターの編集
4. 音声合成＆電子書籍ファイルが完成するまで放置
5. できた電子書籍ファイルをお好みのリーダで読む

## 注力したポイント

- WinUI3を用いてモダンでわかりやすいデザインのUIを作成しました。
- GPT4を用いて登場人物の推定＋セリフの関連づけを行えるようにしたこと。
- Style-Bert-VITS2を用いてより人間らしい音声で朗読できること。
- 青空文庫やなろうのスクレイビングを行って最適なルビの処理を行います。

## 使用技術

- WinUI 3
- GPT4
- Style-Bert-VITS2
- AngleSharp
- Epub
