using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using OpenCvSharp.Extensions;

namespace ss2409
{
    public partial class Form1 : Form
    {
        private VideoCapture _videoCapture; //カメラキャプチャー用のオブジェクト
        private bool _isCameraRunning = false; //カメラが起動中かどうかのフラグ
        private Mat _frame; //カメラから取得したフレームを保持する
        private int _captureNumber;
        private int _captureInterval;
        private List<Mat> _capturedImages = new List<Mat>(); //キャプチャーした画像を保持する

        // カメラキャリブレーションパラメータを単一のMatとして初期化
        private Mat _cameraMatrix = new Mat(); //カメラ行列を保持
        private Mat _distCoeffs = new Mat(); //歪み係数を保持

        // チェスボードのパラメータ
        private const int _CHESSBOARD_WIDTH = 9;
        private const int _CHESSBOARD_HEIGHT = 6;
        private const float _SQUARE_SIZE = 0.010f; // 1つの四角の大きさ10mm


        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            this.Text = "ArucoMarker GUI";
            this.Icon = new Icon("C:\\Users\\takos\\source\\repos\\ss2409\\ss2409\\favicon.ico");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera();
        }

        // 以下にボタン1の操作を示す
        private void button1_Click(object sender, EventArgs e)
        {
            if (_isCameraRunning)
            {
                StopCamera();
                button_start_end.Text = "開始";
            }
            else
            {
                if (int.TryParse(comboBox1.SelectedItem?.ToString(), out int captureNumber))
                {
                    _captureNumber = captureNumber;
                }
                else
                {
                    MessageBox.Show("撮影枚数を正しく選択してください。");
                    return;
                }

                if (int.TryParse(comboBox2.SelectedItem?.ToString(), out int captureInterval))
                {
                    _captureInterval = captureInterval;
                }
                else
                {
                    MessageBox.Show("撮影間隔を正しく選択してください。");
                    return;
                }
                StartCamera();
                button_start_end.Text = "停止";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(_capturedImages.Count == 0)
            {
                MessageBox.Show("画像が取得されていません");
                return;
            }
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("ファイル名を入力してください");
                return;
            }
            SaveCalibrationResults(_cameraMatrix, _distCoeffs);
        }

        private void StartCamera()
        {
            try
            {
                _videoCapture = new VideoCapture(0);
                if (!_videoCapture.IsOpened())
                {
                    MessageBox.Show("カメラを開けませんでした。");
                    return;
                }

                _isCameraRunning = true;
                Task.Run(() => CaptureFrames());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"カメラの起動中にエラーが発生しました: {ex.Message}");
            }
        }

        private void StopCamera()
        {
            _isCameraRunning = false;
            if (_videoCapture != null && !_videoCapture.IsDisposed)
            {
                _videoCapture.Release();
                _videoCapture.Dispose();
                _videoCapture = null;
            }
            Invoke(new Action(() =>
            {
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }
            }));
        }

        private async void CaptureFrames()
        {
            int frameCount = 0;
            int capturedFrames = 0;
            var allCorners = new List<Point2f[]>();
            var allIds = new List<int>();

            try
            {
                while (_isCameraRunning && capturedFrames < _captureNumber)
                {
                    using (var frame = new Mat())
                    {
                        if (_videoCapture.Read(frame) && !frame.Empty())
                        {
                            frameCount++;
                            var displayFrame = frame.Clone(); // 表示用のフレームをクローン

                            var corners = new Point2f[0];
                            var found = Cv2.FindChessboardCorners(frame, new OpenCvSharp.Size(_CHESSBOARD_WIDTH, _CHESSBOARD_HEIGHT), out corners);

                            if (found && frameCount % _captureInterval == 0)
                            {
                                Cv2.DrawChessboardCorners(displayFrame, new OpenCvSharp.Size(_CHESSBOARD_WIDTH, _CHESSBOARD_HEIGHT), corners, found);
                                _capturedImages.Add(frame.Clone());
                                allCorners.Add(corners);
                                capturedFrames++;
                                await UpdateUI(BitmapConverter.ToBitmap(displayFrame), $"キャプチャー {capturedFrames}/{_captureNumber} 完了");
                            }
                            else
                            {
                                // キャプチャーしない場合でも画像を表示
                                if (found)
                                {
                                    Cv2.DrawChessboardCorners(displayFrame, new OpenCvSharp.Size(_CHESSBOARD_WIDTH, _CHESSBOARD_HEIGHT), corners, found);
                                }
                                await UpdateUI(BitmapConverter.ToBitmap(displayFrame));
                            }

                            displayFrame.Dispose();
                        }
                    }
                    await Task.Delay(30); // フレームレートを制御（約30FPS）
                }

                // キャプチャーが終わったのでキャリブレーションを実行
                PerformChessboardCalibration(_capturedImages, allCorners);

                // リソース解放
                foreach (var img in _capturedImages)
                {
                    img?.Dispose();
                }
                _capturedImages.Clear();
            }
            catch (Exception ex)
            {
                await Task.Run(() =>
                {
                    Invoke(new Action(() =>
                    {
                        MessageBox.Show($"エラーが発生しました: {ex.Message}");
                        StopCamera();
                    }));
                });
            }
        }

        private async Task UpdateUI(Bitmap bitmap, string labelText = null)
        {
            await Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    if (pictureBox1.Image != null)
                    {
                        var oldImage = pictureBox1.Image;
                        pictureBox1.Image = bitmap;
                        oldImage.Dispose();
                    }
                    else
                    {
                        pictureBox1.Image = bitmap;
                    }

                    if (labelText != null)
                    {
                        label1.Text = labelText;
                    }
                }));
            });
        }

        private void PerformChessboardCalibration(List<Mat> images, List<Point2f[]> allCorners)
        {
            try
            {
                var objPoints = new List<Mat>();
                var imgPoints = new List<Mat>();

                // 3D座標を生成（チェスボードの各コーナー）
                var objP = new List<Point3f>();
                for (int i = 0; i < _CHESSBOARD_HEIGHT; i++)
                {
                    for (int j = 0; j < _CHESSBOARD_WIDTH; j++)
                    {
                        objP.Add(new Point3f(j * _SQUARE_SIZE, i * _SQUARE_SIZE, 0));
                    }
                }

                foreach (var corners in allCorners)
                {
                    var objPointMat = new Mat(objP.Count, 1, MatType.CV_32FC3);
                    objPointMat.SetArray(objP.ToArray());
                    objPoints.Add(objPointMat);

                    var imgPointMat = new Mat(corners.Length, 1, MatType.CV_32FC2);
                    imgPointMat.SetArray(corners);
                    imgPoints.Add(imgPointMat);
                }

                if (objPoints.Count > 0)
                {
                    OpenCvSharp.Size imageSize = images[0].Size();

                    // カメラ行列の初期化
                    _cameraMatrix = Mat.Eye(3, 3, MatType.CV_64FC1);

                    // 歪み係数の初期化（5つのパラメータ用）
                    _distCoeffs = Mat.Zeros(5, 1, MatType.CV_64FC1);

                    Mat[] rvecs, tvecs;

                    double repError = Cv2.CalibrateCamera(
                        objPoints,
                        imgPoints,
                        imageSize,
                        _cameraMatrix,
                        _distCoeffs,
                        out rvecs,
                        out tvecs,
                        CalibrationFlags.RationalModel
                    );

                    MessageBox.Show($"キャリブレーション完了！\n再投影誤差: {repError:F6}");

                    // リソースの解放
                    foreach (var mat in objPoints.Concat(imgPoints).Concat(rvecs).Concat(tvecs))
                    {
                        mat?.Dispose();
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"キャリブレーション中にエラーが発生しました: {ex.Message}");
            }
        }

        private void SaveCalibrationResults(Mat cameraMatrix, Mat distCoeffs)
        {
            try
            {
                string filePath = textBox1.Text + ".csv";
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Camera Matrix:");
                    for (int i = 0; i < cameraMatrix.Rows; i++)
                    {
                        for (int j = 0; j < cameraMatrix.Cols; j++)
                        {
                            writer.Write($"{cameraMatrix.At<double>(i, j)},");
                        }
                        writer.WriteLine();
                    }

                    writer.WriteLine("\nDistortion Coefficients:");
                    for (int i = 0; i < distCoeffs.Rows; i++)
                    {
                        writer.Write($"{distCoeffs.At<double>(i, 0)},");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"キャリブレーション結果の保存中にエラーが発生しました: {ex.Message}");
            }
        }
    }
}