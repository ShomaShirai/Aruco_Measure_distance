using System;
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
        //private Mat _frame;
        private Dictionary _dictionary;
        private DetectorParameters _detectorParameters;
        private int _captureNumber = 20;
        private int _captureInterval = 5;

        public Form1()
        {
            InitializeComponent();
            InitializeAruco();
        }

        // 初期化: Aruco辞書と検出パラメータを設定
        private void InitializeAruco()
        {
            _dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
            _detectorParameters = new DetectorParameters
            {
                AdaptiveThreshWinSizeMin = 3,
                AdaptiveThreshWinSizeMax = 23,
                AdaptiveThreshWinSizeStep = 10,
                MinMarkerPerimeterRate = 0.03,
                MaxMarkerPerimeterRate = 4.0
            };

        }

        // ボタン1: カメラ起動と検出開始
        private void button1_Click(object sender, EventArgs e)
        {
            if (_isCameraRunning)
            {
                StopCamera();
            }
            else
            {
                StartCamera();
            }
        }

        // カメラ起動処理
        private void StartCamera()
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

        // カメラ停止処理
        private void StopCamera()
        {
            _isCameraRunning = false;
            _videoCapture?.Release();
            pictureBox1.Image = null;
        }

        // フレームのキャプチャと表示
        private void CaptureFrames()
        {
            while (_isCameraRunning)
            {
                using (var frame = new Mat())
                {
                    _videoCapture.Read(frame);
                    if (frame.Empty())
                        continue;

                    using (Mat detectedFrame = DetectArucoMarkers(frame.Clone()))
                    {
                        Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(detectedFrame);
                        Invoke(new Action(() =>
                        {
                            pictureBox1.Image?.Dispose();
                            pictureBox1.Image = bitmap;
                        }));
                    }
                }
            }
        }

        // Arucoマーカーの検出と姿勢推定
        private Mat DetectArucoMarkers(Mat frame)
        {
            Mat grayFrame = new Mat();
            Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

            Point2f[][] corners;
            int[] ids;
            Point2f[][] rejectedCandidates;

            CvAruco.DetectMarkers(
                grayFrame,
                _dictionary,
                out corners,
                out ids,
                _detectorParameters,
                out rejectedCandidates
            );

            if (ids != null && ids.Length > 0)
            {
                // 検出されたマーカーを描画
                CvAruco.DrawDetectedMarkers(frame, corners, ids);

                // カメラパラメータと歪み係数の設定
                var cameraMatrix = InputArray.Create(new double[,]
                {
                    { 1000, 0, frame.Width / 2 },
                    { 0, 1000, frame.Height / 2 },
                    { 0, 0, 1 }
                });

                var distCoeffs = InputArray.Create(new double[] { 0, 0, 0, 0, 0 });

                for (int i = 0; i < ids.Length; i++)
                {
                    // OutputArrayのインスタンスを作成するために、配列を使用します
                    Mat rvec = new Mat();
                    Mat tvec = new Mat();

                    // 姿勢推定
                    CvAruco.EstimatePoseSingleMarkers(
                        new Point2f[][] { corners[i] }, // Point2f[][] 型に変換
                        0.05f, // マーカーの実寸（メートル単位）
                        cameraMatrix,
                        distCoeffs,
                        rvec,
                        tvec
                    );

                    // マーカーの姿勢を描画
                    DrawAxisManual(frame, cameraMatrix, distCoeffs, rvec, tvec, 0.1f);
                }
            }
            return frame;
        }

        //マーカの姿勢を描画する関数
        private void DrawAxisManual(Mat image, InputArray cameraMatrix, InputArray distCoeffs, Mat rvec, Mat tvec, float length)
        {
            // 座標軸の3Dポイントを定義
            var axisPoints = new Mat(4, 1, MatType.CV_32FC3);
            axisPoints.Set(0, new Point3f(0, 0, 0));       // 原点
            axisPoints.Set(1, new Point3f(length, 0, 0));  // X軸
            axisPoints.Set(2, new Point3f(0, length, 0));  // Y軸
            axisPoints.Set(3, new Point3f(0, 0, -length)); // Z軸

            // 2D投影されたポイントを格納する Mat を作成
            var imagePoints = new Mat();

            // 3Dポイントを画像平面上に投影
            Cv2.ProjectPoints(
                axisPoints,
                rvec,
                tvec,
                cameraMatrix,
                distCoeffs,
                imagePoints
            );

            // 投影結果をPoint2fの配列に変換
            Point2f[] points = new Point2f[imagePoints.Rows];
            for (int i = 0; i < imagePoints.Rows; i++)
            {
                points[i] = imagePoints.At<Point2f>(i);
            }

            // Point2fからOpenCvSharp.Pointに明示的に変換して描画
            Cv2.Line(image,
                new OpenCvSharp.Point((int)points[0].X, (int)points[0].Y),
                new OpenCvSharp.Point((int)points[1].X, (int)points[1].Y),
                Scalar.Red, 2);   // X軸
            Cv2.Line(image,
                new OpenCvSharp.Point((int)points[0].X, (int)points[0].Y),
                new OpenCvSharp.Point((int)points[2].X, (int)points[2].Y),
                Scalar.Green, 2); // Y軸
            Cv2.Line(image,
                new OpenCvSharp.Point((int)points[0].X, (int)points[0].Y),
                new OpenCvSharp.Point((int)points[3].X, (int)points[3].Y),
                Scalar.Blue, 2);  // Z軸
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
