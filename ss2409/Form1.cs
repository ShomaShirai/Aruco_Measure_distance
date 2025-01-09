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

namespace ss2409
{
    public partial class Form1 : Form
    {
        private VideoCapture _videoCapture; //カメラキャプチャー用のオブジェクト
        private bool _isCameraRunning = false; //カメラが起動中かどうかのフラグ
        private Mat _frame; //カメラから取得したフレームを保持する
        private int _captureNumber; // キャプチャー数
        private int _captureInterval; // キャプチャー間隔
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
        private float MARKER_SIZE; // マーカーのサイズ 2cm
        private Dictionary _dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
        private Point3d _latestMarkerPosition; // 最新のマーカー位置を保持するフィールドを追加
        private float _stickLength; // 指示棒の長さを10cmとして設定

        // 面積測定で用いるパラメータ (最初の三点で法線ベクトルを計算しているのででこぼこでは計算しにくい)
        private List<Point3d> _vertexPoints = new List<Point3d>();　// 頂点を保持するリスト
        private bool _isCollectingVertices = false; // 頂点収集中かのフラグ

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
            // ComboBox3の値を取得してマーカサイズを設定
            if (comboBox3.SelectedItem != null && int.TryParse(comboBox3.SelectedItem.ToString(), out int markerSize))
            {
                MARKER_SIZE = (float)(markerSize / 100.0);
                Console.WriteLine($"MARKER_SIZE: {MARKER_SIZE}");
            }
            else
            {
                MessageBox.Show("Arucoマーカサイズを正しく選択してください。");
                return;
            }

            // ComboBox4の値を取得して矢印の距離を設定
            if (comboBox4.SelectedItem != null && int.TryParse(comboBox4.SelectedItem.ToString(), out int stickLength))
            {
                _stickLength = (float)(stickLength / 100.0);
                Console.WriteLine($"_stickLength: {_stickLength}");
            }
            else
            {
                MessageBox.Show("Arucoマーカと矢印の距離を正しく選択してください。");
                return;
            }

            // 計測処理の開始または停止
            if (_isMeasuring)
            {
                if (_measurementPoints.Count == 0)
                {
                    // 1点目を記録
                    CaptureFirstPoint();
                }
                else
                {
                    // 計測を停止
                    StopMeasurement();
                    button_Measure.Text = "計測開始";
                }
            }
            else
            {
                // 計測を開始
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
                button_Measure.Text = "二点目決定";
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

                                // 並進ベクトルと回転ベクトルを代入
                                Vec3d[] rvecArray = new Vec3d[1];
                                Vec3d[] tvecArray = new Vec3d[1];
                                rvecsAruco.GetArray(out rvecArray);
                                tvecsAruco.GetArray(out tvecArray);

                                // マーカーの中心位置の決定
                                Point3d markerPosition = new Point3d(
                                    tvecArray[0].Item0,
                                    tvecArray[0].Item1,
                                    tvecArray[0].Item2
                                );

                                // ロドリゲスの回転行列を計算
                                Mat rotationMatrix = new Mat();
                                Cv2.Rodrigues(rvecArray[0], rotationMatrix);

                                // 指示棒の長さ分だけy方向にポイントの位置を調整
                                Point3d tipOffsetY = new Point3d(
                                     rotationMatrix.At<double>(0, 1) * _stickLength,
                                     rotationMatrix.At<double>(1, 1) * _stickLength,
                                     rotationMatrix.At<double>(2, 1) * _stickLength
                                );

                                _latestMarkerPosition = new Point3d(
                                    markerPosition.X + tipOffsetY.X,
                                    markerPosition.Y + tipOffsetY.Y,
                                    markerPosition.Z + tipOffsetY.Z
                                );

                                // マウスクリックで2点目を計測(ボタン以外を押しても長さが計測できるようになっている)
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

        // ピンボールカメラモデル(3D空間のポイントを2D画像平面に投影する)
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

        //二点間の距離を計算する
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

        private void button_measure_top_Click(object sender, EventArgs e)
        {
            // ComboBox7の値を取得してマーカサイズを設定
            if (comboBox7.SelectedItem != null && int.TryParse(comboBox3.SelectedItem.ToString(), out int markerSize))
            {
                MARKER_SIZE = (float)(markerSize / 100.0);
                Console.WriteLine($"MARKER_SIZE: {MARKER_SIZE}");
            }
            else
            {
                MessageBox.Show("Arucoマーカサイズを正しく選択してください。");
                return;
            }

            // ComboBox8の値を取得して矢印の距離を設定
            if (comboBox8.SelectedItem != null && int.TryParse(comboBox4.SelectedItem.ToString(), out int stickLength))
            {
                _stickLength = (float)(stickLength / 100.0);
                Console.WriteLine($"_stickLength: {_stickLength}");
            }
            else
            {
                MessageBox.Show("Arucoマーカと矢印の距離を正しく選択してください。");
                return;
            }

            if (_isCollectingVertices)
            {
                // 頂点を追加
                if (_latestMarkerPosition != null)
                {
                    _vertexPoints.Add(_latestMarkerPosition);
                    System.Media.SystemSounds.Beep.Play();
                    label15.Text = $"頂点{_vertexPoints.Count}個目を追加しました。次の頂点を決定してください。";
                }
            }
            else
            {
                // 頂点収集開始
                StartVertexCollection();
                button_measure_top.Text = "頂点追加";
            }
        }

        private void button_measure_area_Click(object sender, EventArgs e)
        {
            if (_vertexPoints.Count < 3)
            {
                MessageBox.Show("面積を計算するには少なくとも3点が必要です。");
                return;
            }

            double area = CalculatePolygonArea(_vertexPoints);
            double areaCM2 = area * 10000; // m² から cm² に変換
            textBox3.Text = $"{areaCM2:F2}";

            StopVertexCollection();
            _vertexPoints.Clear();
            button_measure_top.Text = "頂点収集";
        }

        private void StartVertexCollection()
        {
            try
            {
                _videoCapture = new VideoCapture(0);
                if (!_videoCapture.IsOpened())
                {
                    MessageBox.Show("カメラを開けませんでした。");
                    return;
                }
                _isCollectingVertices = true;
                _vertexPoints.Clear();
                _latestMarkerPosition = new Point3d();
                Task.Run(() => VertexFrameCapture());
                label15.Text = "頂点を追加するには頂点追加ボタンを押してください";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"頂点収集開始時にエラーが発生しました: {ex.Message}");
            }
        }

        private void StopVertexCollection()
        {
            _isCollectingVertices = false;
            if (_videoCapture != null && !_videoCapture.IsDisposed)
            {
                _videoCapture.Release();
                _videoCapture.Dispose();
                _videoCapture = null;
            }
            label15.Text = "頂点収集が終了しました。新しい計測を開始するには頂点収集を押してください";
        }

        private async void VertexFrameCapture()
        {
            try
            {
                while (_isCollectingVertices)
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

                                Point3d markerPosition = new Point3d(
                                    tvecArray[0].Item0,
                                    tvecArray[0].Item1,
                                    tvecArray[0].Item2
                                );

                                Mat rotationMatrix = new Mat();
                                Cv2.Rodrigues(rvecArray[0], rotationMatrix);

                                Point3d tipOffsetY = new Point3d(
                                     rotationMatrix.At<double>(0, 1) * _stickLength,
                                     rotationMatrix.At<double>(1, 1) * _stickLength,
                                     rotationMatrix.At<double>(2, 1) * _stickLength
                                );

                                _latestMarkerPosition = new Point3d(
                                    markerPosition.X + tipOffsetY.X,
                                    markerPosition.Y + tipOffsetY.Y,
                                    markerPosition.Z + tipOffsetY.Z
                                );

                                rotationMatrix.Dispose();
                                rvecsAruco.Dispose();
                                tvecsAruco.Dispose();
                            }

                            // 収集した頂点の描画と線の描画
                            if (_vertexPoints.Count > 0)
                            {
                                // 頂点の描画
                                foreach (var point in _vertexPoints)
                                {
                                    Point2d imagePoint = ProjectPoint(point, _cameraMatrixFromFile);
                                    Cv2.Circle(displayFrame, (OpenCvSharp.Point)imagePoint, 5, Scalar.Red, -1);
                                }

                                // 頂点を結ぶ線の描画
                                for (int i = 0; i < _vertexPoints.Count; i++)
                                {
                                    Point2d current = ProjectPoint(_vertexPoints[i], _cameraMatrixFromFile);
                                    Point2d next = ProjectPoint(_vertexPoints[(i + 1) % _vertexPoints.Count], _cameraMatrixFromFile);
                                    Cv2.Line(displayFrame, (OpenCvSharp.Point)current, (OpenCvSharp.Point)next, Scalar.Blue, 2);
                                }
                            }

                            await UpdateThirdUI(BitmapConverter.ToBitmap(displayFrame));

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
                        MessageBox.Show($"頂点収集中にエラーが発生しました: {ex.Message}");
                        StopVertexCollection();
                    }));
                });
            }
        }

        private double CalculatePolygonArea(List<Point3d> vertices)
        {
            // 3D空間での多角形の面積を計算
            if (vertices.Count < 3) return 0;

            // 多角形の法線ベクトルを計算
            Point3d normal = CalculateNormalVector(vertices);

            // 基準となる平面を決定（最も適切な投影平面を選択）
            string projectionPlane = DetermineProjectionPlane(normal);

            // 選択された平面に投影して面積を計算
            double area = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                Point3d current = vertices[i];
                Point3d next = vertices[(i + 1) % vertices.Count];

                switch (projectionPlane)
                {
                    case "xy":
                        area += (current.X * next.Y - next.X * current.Y);
                        break;
                    case "yz":
                        area += (current.Y * next.Z - next.Y * current.Z);
                        break;
                    case "xz":
                        area += (current.X * next.Z - next.X * current.Z);
                        break;
                }
            }

            area = Math.Abs(area) / 2;

            // 実際の面積に補正（投影による歪みを補正）
            double normalLength = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            switch (projectionPlane)
            {
                case "xy":
                    area /= Math.Abs(normal.Z) / normalLength;
                    break;
                case "yz":
                    area /= Math.Abs(normal.X) / normalLength;
                    break;
                case "xz":
                    area /= Math.Abs(normal.Y) / normalLength;
                    break;
            }

            return area;
        }

        private Point3d CalculateNormalVector(List<Point3d> vertices)
        {
            // 最初の3点から法線ベクトルを計算
            Point3d v1 = new Point3d(
                vertices[1].X - vertices[0].X,
                vertices[1].Y - vertices[0].Y,
                vertices[1].Z - vertices[0].Z
            );

            Point3d v2 = new Point3d(
                vertices[2].X - vertices[0].X,
                vertices[2].Y - vertices[0].Y,
                vertices[2].Z - vertices[0].Z
            );

            // 外積を計算
            return new Point3d(
                v1.Y * v2.Z - v1.Z * v2.Y,
                v1.Z * v2.X - v1.X * v2.Z,
                v1.X * v2.Y - v1.Y * v2.X
            );
        }

        private string DetermineProjectionPlane(Point3d normal)
        {
            // 法線ベクトルの各成分の絶対値を比較
            double absX = Math.Abs(normal.X);
            double absY = Math.Abs(normal.Y);
            double absZ = Math.Abs(normal.Z);

            // 最も大きい成分に垂直な平面を選択
            if (absZ >= absX && absZ >= absY)
                return "xy";
            else if (absX >= absY && absX >= absZ)
                return "yz";
            else
                return "xz";
        }

        // 画像の更新
        private async Task UpdateThirdUI(Bitmap bitmap)
        {
            await Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    if (pictureBox3.Image != null)
                    {
                        var oldImage = pictureBox3.Image;
                        pictureBox3.Image = bitmap;
                        oldImage.Dispose();
                    }
                    else
                    {
                        pictureBox3.Image = bitmap;
                    }
                }));
            });
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }
    }
}