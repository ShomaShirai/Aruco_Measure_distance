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
        private VideoCapture _videoCapture;
        private bool _isCameraRunning = false;
        private Mat _frame;
        private Dictionary _dictionary;
        private DetectorParameters _detectorParameters;
        private int _captureNumber;
        private int _captureInterval;
        private const float MARKER_SIZE = 0.02f;

        public Form1()
        {
            InitializeComponent();
            InitializeAruco();
            this.FormClosing += Form1_FormClosing;
            this.Text = "Arucoマーカ GUI";
            this.Icon = new Icon("C:\\Users\\takos\\source\\repos\\ss2409\\ss2409\\favicon.ico");
        }

        private void InitializeAruco()
        {
            _dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
            _detectorParameters = new DetectorParameters();
            _detectorParameters.AdaptiveThreshConstant = 7;
            _detectorParameters.MinMarkerPerimeterRate = 0.03;
            _detectorParameters.MaxMarkerPerimeterRate = 4.0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_isCameraRunning)
            {
                StopCamera();
                button1.Text = "開始";
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
                button1.Text = "停止";
            }
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
            var capturedImages = new List<Mat>();
            var markerCorners = new List<Point2f[]>();
            var markerIds = new List<int>();

            try
            {
                while (_isCameraRunning && capturedFrames < _captureNumber)
                {
                    using (var frame = new Mat())
                    {
                        if (_videoCapture.Read(frame) && !frame.Empty())
                        {
                            frameCount++;

                            // Arucoマーカーの検出
                            Point2f[][] corners;
                            int[] ids;
                            DetectMarkers(frame, out corners, out ids);

                            if (ids != null && ids.Length > 0)
                            {
                                // マーカーを検出した場合、フレームに描画
                                CvAruco.DrawDetectedMarkers(frame, corners, ids);

                                // 指示棒の先端を計算して描画
                                foreach (var corner in corners)
                                {
                                    var pointerTip = CalculatePointerTip(corner);
                                    Cv2.Circle(frame, (OpenCvSharp.Point)pointerTip, 5, Scalar.Red, -1);
                                }

                                if (frameCount % _captureInterval == 0)
                                {
                                    capturedImages.Add(frame.Clone());
                                    markerCorners.AddRange(corners);
                                    markerIds.AddRange(ids);
                                    var capturedBitmap = BitmapConverter.ToBitmap(frame);
                                    await UpdateUI(capturedBitmap, $"撮影済み: {++capturedFrames}/{_captureNumber}枚");
                                }
                            }

                            var previewBitmap = BitmapConverter.ToBitmap(frame);
                            await UpdateUI(previewBitmap);
                        }
                    }
                }

                if (capturedImages.Count >= _captureNumber)
                {
                    using (var calibrationResult = PerformArucoCalibration(capturedImages, markerCorners, markerIds))
                    {
                        if (calibrationResult != null)
                        {
                            var calibrationBitmap = BitmapConverter.ToBitmap(calibrationResult);
                            await UpdateUI(calibrationBitmap, "キャリブレーション完了");
                        }
                    }
                }

                // リソース解放
                foreach (var img in capturedImages)
                {
                    img.Dispose();
                }
                capturedImages.Clear();

                // 継続的なプレビュー表示（キャリブレーション結果を適用）
                while (_isCameraRunning)
                {
                    using (var frame = new Mat())
                    {
                        if (_videoCapture.Read(frame) && !frame.Empty())
                        {
                            // マーカー検出とポインタ先端の表示
                            Point2f[][] corners;
                            int[] ids;
                            DetectMarkers(frame, out corners, out ids);

                            if (ids != null && ids.Length > 0)
                            {
                                CvAruco.DrawDetectedMarkers(frame, corners, ids);
                                foreach (var corner in corners)
                                {
                                    var pointerTip = CalculatePointerTip(corner);
                                    Cv2.Circle(frame, (OpenCvSharp.Point)pointerTip, 5, Scalar.Red, -1);
                                }
                            }

                            var previewBitmap = BitmapConverter.ToBitmap(frame);
                            await UpdateUI(previewBitmap);
                        }
                    }
                }
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

        private void DetectMarkers(Mat frame, out Point2f[][] corners, out int[] ids)
        {
            using (var gray = new Mat())
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Point2f[][] rejectedImgPoints;
                CvAruco.DetectMarkers(gray, _dictionary, out corners, out ids, _detectorParameters, out rejectedImgPoints);
            }
        }

        private Point2f CalculatePointerTip(Point2f[] markerCorners)
        {
            // マーカーの中心を計算
            var centerX = markerCorners.Average(p => p.X);
            var centerY = markerCorners.Average(p => p.Y);

            // マーカーの向きを計算（上辺の中点から下辺の中点への方向）
            var topMidPoint = new Point2f(
                (markerCorners[0].X + markerCorners[1].X) / 2,
                (markerCorners[0].Y + markerCorners[1].Y) / 2
            );
            var bottomMidPoint = new Point2f(
                (markerCorners[2].X + markerCorners[3].X) / 2,
                (markerCorners[2].Y + markerCorners[3].Y) / 2
            );

            // 方向ベクトルを計算
            var dirX = bottomMidPoint.X - topMidPoint.X;
            var dirY = bottomMidPoint.Y - topMidPoint.Y;

            // ベクトルを正規化して指示棒の長さ分延長
            float length = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            float pointerLength = length * 2; // 指示棒の長さをマーカーサイズの2倍と仮定

            return new Point2f(
                centerX + (dirX / length) * pointerLength,
                centerY + (dirY / length) * pointerLength
            );
        }

        private Mat PerformArucoCalibration(List<Mat> images, List<Point2f[]> allCorners, List<int> allIds)
        {
            try
            {
                var objPoints = new List<Mat>();
                var imgPoints = new List<Mat>();

                // 3D座標を生成（マーカーの四隅）
                var markerObjPoints = new Point3f[]
                {
                    new Point3f(-MARKER_SIZE/2f, MARKER_SIZE/2f, 0),
                    new Point3f(MARKER_SIZE/2f, MARKER_SIZE/2f, 0),
                    new Point3f(MARKER_SIZE/2f, -MARKER_SIZE/2f, 0),
                    new Point3f(-MARKER_SIZE/2f, -MARKER_SIZE/2f, 0)
                };

                // 検出された各マーカーについて3D-2D対応点を収集
                for (int i = 0; i < allIds.Count; i++)
                {
                    var objPointMat = new Mat(markerObjPoints.Length, 1, MatType.CV_32FC3);
                    objPointMat.SetArray(markerObjPoints);
                    objPoints.Add(objPointMat);

                    var imgPointMat = new Mat(allCorners[i].Length, 1, MatType.CV_32FC2);
                    imgPointMat.SetArray(allCorners[i]);
                    imgPoints.Add(imgPointMat);
                }

                if (objPoints.Count > 0)
                {
                    var cameraMatrix = new Mat();
                    var distCoeffs = new Mat();
                    Mat[] rvecs, tvecs;

                    OpenCvSharp.Size imageSize = images[0].Size();
                    double repError = Cv2.CalibrateCamera(
                        objPoints,
                        imgPoints,
                        imageSize,
                        cameraMatrix,
                        distCoeffs,
                        out rvecs,
                        out tvecs,
                        CalibrationFlags.RationalModel
                    );

                    SaveCalibrationResults(cameraMatrix, distCoeffs);
                    MessageBox.Show($"キャリブレーション完了！\n再投影誤差: {repError:F6}");

                    // 最後の画像を補正して返す
                    var undistorted = new Mat();
                    Cv2.Undistort(images.Last(), undistorted, cameraMatrix, distCoeffs);
                    return undistorted;
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"キャリブレーション中にエラーが発生しました: {ex.Message}");
                return null;
            }
        }

        private void SaveCalibrationResults(Mat cameraMatrix, Mat distCoeffs)
        {
            try
            {
                string filePath = "calibration_results.csv";
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
                        writer.WriteLine(distCoeffs.At<double>(i, 0));
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