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
using System.Collections;

// 計測で正しい値を計算できるようにする

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

        // csvファイルから読み取ったカメラ行列と歪み係数
        private Mat _cameraMatrixFromFile = new Mat();
        private Mat _distCoeffsFromFile = new Mat();

        // チェスボードのパラメータ
        private const int _CHESSBOARD_WIDTH = 9;
        private const int _CHESSBOARD_HEIGHT = 6;
        private const float _SQUARE_SIZE = 0.010f; // 1つの四角の大きさ10mm

        // Arucoマーカのパラメータ
        private List<Point3d> _measurementPoints = new List<Point3d>();
        private bool _isMeasuring = false;
        private const float MARKER_SIZE = 0.02f; // マーカーのサイズ 2cm
        private Dictionary _dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
        private Point3d _latestMarkerPosition; // 最新のマーカー位置を保持するフィールドを追加

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
        private void button_start_end_Click(object sender, EventArgs e)
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
                button_start_end.Text = "終了";
            }
        }

        private void button_reserve_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("ファイル名を入力してください");
                return;
            }
            SaveCalibrationResults(_cameraMatrix, _distCoeffs);
            MessageBox.Show($"{textBox2.Text}.csvファイルに保存が完了しました\n次にキャリブレーションを行いましょう!");
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

        // キャリブレーションのためにフレームをキャプチャーする関数
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
                                await UpdateFirstUI(BitmapConverter.ToBitmap(displayFrame), $"キャプチャー {capturedFrames}/{_captureNumber} 完了");
                            }
                            else
                            {
                                // キャプチャーしない場合でも画像を表示
                                if (found)
                                {
                                    Cv2.DrawChessboardCorners(displayFrame, new OpenCvSharp.Size(_CHESSBOARD_WIDTH, _CHESSBOARD_HEIGHT), corners, found);
                                }
                                await UpdateFirstUI(BitmapConverter.ToBitmap(displayFrame));
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

        private async Task UpdateFirstUI(Bitmap bitmap, string labelText = null)
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

                    // 歪み係数の初期化（8つのパラメータ用）
                    _distCoeffs = Mat.Zeros(8, 1, MatType.CV_64FC1);

                    Mat[] rvecsChessboard, tvecsChessboard;

                    double repError = Cv2.CalibrateCamera(
                        objPoints,
                        imgPoints,
                        imageSize,
                        _cameraMatrix,
                        _distCoeffs,
                        out rvecsChessboard,
                        out tvecsChessboard,
                        CalibrationFlags.RationalModel
                    );

                    MessageBox.Show($"キャリブレーション完了！\n再投影誤差: {repError:F6}");

                    // リソースの解放
                    foreach (var mat in objPoints.Concat(imgPoints).Concat(rvecsChessboard).Concat(tvecsChessboard))
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
                string filePath = $"{textBox2.Text}.csv";

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

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        // CSVファイルを読み込んでカメラ行列と歪み係数を設定
        private void button_select_file_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedFilePath = openFileDialog1.FileName;
                try
                {
                    // CSVファイルを読み込む
                    using (var reader = new StreamReader(selectedFilePath))
                    {
                        // カメラ行列の読み込み
                        string header = reader.ReadLine(); // "cameramatrix" を読み飛ばす

                        _cameraMatrixFromFile = Mat.Zeros(3, 3, MatType.CV_64FC1);
                        for (int i = 0; i < 3; i++)
                        {
                            string[] values = reader.ReadLine().TrimEnd(',').Split(',');
                            for (int j = 0; j < 3; j++)
                            {
                                _cameraMatrixFromFile.Set<double>(i, j, double.Parse(values[j]));
                            }
                        }

                        reader.ReadLine(); // 改行を読み飛ばす
                        reader.ReadLine(); // "diffcoffs" を読み飛ばす

                        // 歪み係数の読み込み
                        string[] distValues = reader.ReadLine().TrimEnd(',').Split(',');
                        _distCoeffsFromFile = Mat.Zeros(8, 1, MatType.CV_64FC1);
                        for (int i = 0; i < distValues.Length; i++)
                        {
                            _distCoeffsFromFile.Set<double>(i, 0, double.Parse(distValues[i]));
                        }

                        Console.WriteLine(_distCoeffsFromFile);
                    }

                    MessageBox.Show("カメラパラメータの読み込みが完了しました。");
                    label6.Text = "[一点目]指示棒を対象に当てて計測ボタンを押してください";

                    // カメラが起動中の場合は、歪み補正処理を開始
                    if (_isCameraRunning)
                    {
                        // 既存のカメラ行列と歪み係数を更新
                        _cameraMatrix = _cameraMatrixFromFile.Clone();
                        _distCoeffs = _distCoeffsFromFile.Clone();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ファイルの読み込み中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        private void button_Measure_Click(object sender, EventArgs e)
        {
            if (_isMeasuring)
            {
                if (_measurementPoints.Count == 0) // まだ1点目が計測されていない場合
                {
                    // 現在のマーカー位置を1点目として記録
                    CaptureFirstPoint();
                }
                else // 1点目が既に計測されている場合
                {
                    StopMeasurement();
                    button_Measure.Text = "計測開始";
                }
            }
            else
            {
                StartMeasurement();
                button_Measure.Text = "1点目決定";
            }
        }

        private void StartMeasurement()
        {
            try
            {
                _videoCapture = new VideoCapture(0);
                if (!_videoCapture.IsOpened())
                {
                    MessageBox.Show("カメラを開けませんでした。");
                    return;
                }
                _isMeasuring = true;
                _measurementPoints.Clear();
                _latestMarkerPosition = new Point3d();
                Task.Run(() => MeasurementFrameCapture());
                label6.Text = "計測ボタンを押して1点目を計測してください";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"計測開始時にエラーが発生しました: {ex.Message}");
            }
        }

        private void StopMeasurement()
        {
            _isMeasuring = false;
            if (_videoCapture != null && !_videoCapture.IsDisposed)
            {
                _videoCapture.Release();
                _videoCapture.Dispose();
                _videoCapture = null;
            }
            _measurementPoints.Clear();
            label6.Text = "計測が終了しました。新しい計測を開始するには計測開始を押してください";
        }

        private void CaptureFirstPoint()
        {
            if (_latestMarkerPosition != null)
            {
                _measurementPoints.Add(_latestMarkerPosition);
                System.Media.SystemSounds.Beep.Play();
                label6.Text = "画面をクリックして2点目を計測してください";
                button_Measure.Text = "計測終了";
            }
        }

        private async void MeasurementFrameCapture()
        {
            try
            {
                while (_isMeasuring)
                {
                    using (var frame = new Mat())
                    {
                        if (_videoCapture.Read(frame) && !frame.Empty())
                        {
                            Mat undistorted = new Mat();
                            Cv2.Undistort(frame, undistorted, _cameraMatrixFromFile, _distCoeffsFromFile);
                            var displayFrame = undistorted.Clone();

                            Point2f[][] corners;
                            int[] ids;
                            DetectorParameters detectorParams = new DetectorParameters();
                            CvAruco.DetectMarkers(undistorted, _dictionary, out corners, out ids, detectorParams, out _);

                            if (ids != null && ids.Length > 0)
                            {
                                // 最初に検出されたマーカーのみを使用
                                CvAruco.DrawDetectedMarkers(displayFrame, corners, ids);

                                Mat rvecsAruco = new Mat();
                                Mat tvecsAruco = new Mat();

                                CvAruco.EstimatePoseSingleMarkers(
                                    corners,
                                    MARKER_SIZE,
                                    _cameraMatrixFromFile,
                                    _distCoeffsFromFile,
                                    rvecsAruco,
                                    tvecsAruco
                                );

                                Vec3d[] rvecArray = new Vec3d[1];
                                Vec3d[] tvecArray = new Vec3d[1];
                                rvecsAruco.GetArray(out rvecArray);
                                tvecsAruco.GetArray(out tvecArray);

                                // マーカー位置の更新
                                Point3d markerPosition = new Point3d(
                                    tvecArray[0].Item0,
                                    tvecArray[0].Item1,
                                    tvecArray[0].Item2
                                );

                                Mat rotationMatrix = new Mat();
                                Cv2.Rodrigues(rvecArray[0], rotationMatrix);

                                Point3d tipOffset = new Point3d(
                                    rotationMatrix.At<double>(0, 2) * 0.05,
                                    rotationMatrix.At<double>(1, 2) * 0.05,
                                    rotationMatrix.At<double>(2, 2) * 0.05
                                );

                                _latestMarkerPosition = new Point3d(
                                    markerPosition.X + tipOffset.X,
                                    markerPosition.Y + tipOffset.Y,
                                    markerPosition.Z + tipOffset.Z
                                );

                                // マウスクリックで2点目を計測
                                if (_measurementPoints.Count == 1 && MouseButtons == MouseButtons.Left)
                                {
                                    _measurementPoints.Add(_latestMarkerPosition);
                                    System.Media.SystemSounds.Beep.Play();
                                }

                                rotationMatrix.Dispose();
                                rvecsAruco.Dispose();
                                tvecsAruco.Dispose();
                            }

                            // 計測点の描画と距離の計算
                            if (_measurementPoints.Count > 0)
                            {
                                foreach (var point in _measurementPoints)
                                {
                                    Point2d imagePoint = ProjectPoint(point, _cameraMatrixFromFile);
                                    Cv2.Circle(displayFrame, (OpenCvSharp.Point)imagePoint, 5, Scalar.Red, -1);
                                }

                                if (_measurementPoints.Count == 2)
                                {
                                    double distance = CalculateDistance(_measurementPoints[0], _measurementPoints[1]);
                                    double distanceCM = distance * 100;
                                    await UpdateDistanceText(distanceCM);
                                }
                            }

                            await UpdateSecondUI(BitmapConverter.ToBitmap(displayFrame));

                            displayFrame.Dispose();
                            undistorted.Dispose();
                        }
                    }
                    await Task.Delay(30);
                }
            }
            catch (Exception ex)
            {
                await Task.Run(() =>
                {
                    Invoke(new Action(() =>
                    {
                        MessageBox.Show($"計測中にエラーが発生しました: {ex.Message}");
                        StopMeasurement();
                    }));
                });
            }
        }

        private Point2d ProjectPoint(Point3d point3d, Mat cameraMatrix)
        {
            double fx = cameraMatrix.At<double>(0, 0);
            double fy = cameraMatrix.At<double>(1, 1);
            double cx = cameraMatrix.At<double>(0, 2);
            double cy = cameraMatrix.At<double>(1, 2);

            return new Point2d(
                (point3d.X * fx / point3d.Z) + cx,
                (point3d.Y * fy / point3d.Z) + cy
            );
        }

        private double CalculateDistance(Point3d p1, Point3d p2)
        {
            return Math.Sqrt(
                Math.Pow(p2.X - p1.X, 2) +
                Math.Pow(p2.Y - p1.Y, 2) +
                Math.Pow(p2.Z - p1.Z, 2)
            );
        }

        private async Task UpdateSecondUI(Bitmap bitmap)
        {
            await Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    if (pictureBox2.Image != null)
                    {
                        var oldImage = pictureBox2.Image;
                        pictureBox2.Image = bitmap;
                        oldImage.Dispose();
                    }
                    else
                    {
                        pictureBox2.Image = bitmap;
                    }
                }));
            });
        }

        //　二点間距離の出力
        private async Task UpdateDistanceText(double distanceCM)
        {
            await Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    textBox1.Text = $"{distanceCM:F2}";
                }));
            });
        }

    }
}