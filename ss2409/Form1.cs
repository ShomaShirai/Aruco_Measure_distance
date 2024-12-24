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

        public Form1()
        {
            InitializeComponent();
            InitializeAruco();
        }

        private void InitializeAruco()
        {
            _dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
            _detectorParameters = new DetectorParameters();
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

        private void CaptureFrames()
        {
            int frameCount = 0;
            int capturedFrames = 0;
            var capturedImages = new List<Mat>();

            try
            {
                while (_isCameraRunning && capturedFrames < _captureNumber)
                {
                    using (var frame = new Mat())
                    {
                        if (_videoCapture.Read(frame) && !frame.Empty())
                        {
                            frameCount++;

                            if (frameCount % _captureInterval == 0)
                            {
                                capturedImages.Add(frame.Clone());
                                var bitmap = BitmapConverter.ToBitmap(frame);
                                UpdateUI(bitmap, $"撮影済み: {++capturedFrames}/{_captureNumber}枚").Wait();
                            }
                            else
                            {
                                var bitmap = BitmapConverter.ToBitmap(frame);
                                UpdateUI(bitmap).Wait();
                            }
                        }
                    }
                }

                if (capturedImages.Count >= _captureNumber)
                {
                    using (var calibrationResult = PerformCalibration(capturedImages))
                    {
                        if (calibrationResult != null)
                        {
                            var bitmap = BitmapConverter.ToBitmap(calibrationResult);
                            UpdateUI(bitmap, "キャリブレーション完了").Wait();
                        }
                    }
                }

                // リソースの解放
                foreach (var img in capturedImages)
                {
                    img.Dispose();
                }
                capturedImages.Clear();

                // 継続的なプレビュー表示
                while (_isCameraRunning)
                {
                    using (var frame = new Mat())
                    {
                        if (_videoCapture.Read(frame) && !frame.Empty())
                        {
                            var bitmap = BitmapConverter.ToBitmap(frame);
                            UpdateUI(bitmap).Wait();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    MessageBox.Show($"エラーが発生しました: {ex.Message}");
                    StopCamera();
                }));
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

        private Mat PerformCalibration(List<Mat> capturedImages)
        {
            try
            {
                // チェスボードのパターンサイズ（例: 9x6）
                var patternSize = new OpenCvSharp.Size(1, 1);
                var squareSize = 2.0f; // チェスボードの1マスのサイズ（単位：任意）

                var objectPoints = new List<Mat>();
                var imagePoints = new List<Mat>();
                var objectPoint = new List<Point3f>();

                // オブジェクトポイントの準備
                for (int i = 0; i < patternSize.Height; i++)
                {
                    for (int j = 0; j < patternSize.Width; j++)
                    {
                        objectPoint.Add(new Point3f(j * squareSize, i * squareSize, 0));
                    }
                }

                foreach (var image in capturedImages)
                {
                    using (var gray = new Mat())
                    {
                        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

                        if (Cv2.FindChessboardCorners(gray, patternSize, out Point2f[] corners))
                        {
                            var criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.001);
                            Cv2.CornerSubPix(gray, corners, new OpenCvSharp.Size(11, 11), new OpenCvSharp.Size(-1, -1), criteria);

                            imagePoints.Add(Mat.FromArray(corners));
                            objectPoints.Add(Mat.FromArray(objectPoint.ToArray()));
                        }
                    }
                }

                if (objectPoints.Count > 0)
                {
                    var cameraMatrix = new Mat();
                    var distCoeffs = new Mat();
                    Mat[] rvecs, tvecs;

                    OpenCvSharp.Size imageSize = capturedImages[0].Size();
                    double repError = Cv2.CalibrateCamera(objectPoints, imagePoints, imageSize,
                        cameraMatrix, distCoeffs, out rvecs, out tvecs, CalibrationFlags.None);

                    SaveCalibrationResults(cameraMatrix, distCoeffs);
                    MessageBox.Show($"キャリブレーション完了！\n再投影誤差: {repError:F6}");

                    // 最後の画像を補正して返す
                    var undistorted = new Mat();
                    Cv2.Undistort(capturedImages.Last(), undistorted, cameraMatrix, distCoeffs);
                    return undistorted;
                }
                else
                {
                    MessageBox.Show("チェスボードパターンが検出できませんでした。");
                    return null;
                }
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