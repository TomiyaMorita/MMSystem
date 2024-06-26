# 中間管理システム動作テストプログラム使用方法
## ファイルについて
使用するプログラムはすべてMMSystem/Pythonにあります
更新するjsonファイルは、pythonファイルと同じ階層にある必要があります。
### controle.json
自動動作の開始・停止、エラーリセット等動作を変更するときに更新するjsonファイルです。
### nextOrder.json
次に製作するドリンクの情報を更新するjsonファイルです。
### state.json
PLCや中間管理システムの状態により、オーダーシステムが知る必要のあるステータスを更新するjsonファイルです。
### main.py
メインで動かすプログラムです。このプログラムは常に実行されます。
### motionHandler.py
ステータスによって動作を変更・決定するファイル
### demoOrder.py
テスト用にcontrole.json,nextOrder.jsonファイルを書き換える・state.jsonファイルを読み込みステータスの状態を整理するのに使用できます。
### KVELE02mcp.py
PLCにデータを送信する際にmain.pyから呼び出され、変換を行います。
### testPLC.py
PLCが無い環境でも動作テストができるように、PLCの代わりにplcState.jsonを書き換えてテスト動作を行うためのプログラムで、KVELE02mcp.py同様main.pyから呼び出されます。
動作開始・終了、注文受付完了番号、排出完了番号は指示を行うと変化します。

## テスト環境での動作方法
1. main.pyの13行目にあるoperatingModeを1にするとテストモードになります。
2. nextOrder.json,controle.jsonを書き換えるとstate.jsonが命令により書き換えられます。

## 実行環境
Python:3.12.2での動作確認済み
OS:MacOS Monterey 12.4,Windows10・11での動作確認済み
最新バージョンのwatchdogをインストールしてください。[watchdog](https://github.com/gorakhargosh/watchdog)
