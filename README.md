# スキルゼミ課題 SS2409
## 課題名：C# Arucopointer
氏名：白井　正真

## 仕様
チェスボードを用いたカメラキャリブレーションとAruco指示棒を用いた距離計測をできるようにする

## コードの流れ
1. カメラキャリブレーションを行う
2. キャリブレーション結果をcsvファイルに出力する
3. キャリブレーションファイルをcsvファイルから読み取る
4. カメラの歪みをなくす(cv2.Undisortを用いる)
5. Arucoマーカを利用した距離計測を行う

## 開発環境
- Windows 11 WSL2
- Visual studio 2022

## 実行方法
Visual Studio2022でデバックを行う

## 工夫した点
- ファイルが読み込めない場合のエラー処理を行っている
- csvファイルにキャリブレーションデータを保存することで機能性を向上した
- 面積も測定できるようにしている

## 参考資料
- チェッカーボードを用いたキャリブレーションプログラム（OpenCVによる実装）(https://tecsingularity.com/opencv/cameracalib/)
- [VisualStudioの教科書] C#ツールボックス一覧 (https://www.kyoukasho.net/entry/c-sharp-toolbox)
