using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections;
using System.IO.Ports;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ss2410
{
    public partial class Form1 : Form
    {
        private SerialPort _Serial;
        private List<int> btnA = new List<int>();
        private List<float> accXValues = new List<float>();
        private List<float> accYValues = new List<float>();
        private List<float> accZValues = new List<float>();
        private Timer timer;

        private bool isDrawing = false;
        private System.Drawing.Point lastPoint = System.Drawing.Point.Empty;
        private Bitmap drawingLayer = null;

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;

        private float prevX = 0;
        private float prevY = 0;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            this.Text = "Aerial mouse GUI";
            this.Icon = new Icon("C:\\Users\\takos\\source\\repos\\ss2410\\ss2410\\endo.ico");

            // 描画レイヤーの初期化
            drawingLayer = new Bitmap(mouse_picture.Width, mouse_picture.Height);
            using (Graphics g = Graphics.FromImage(drawingLayer))
            {
                g.Clear(Color.Transparent);
            }

            // グラフを描画する
            timer = new Timer();
            timer.Interval = 100; // 0.1秒ごとにグラフに点を打つ
            timer.Tick += Timer_Tick;
            timer.Start();

            // カメラを開始する
            Task.Run(() => ShotImage());

            // シリアル入力を行う
            bool isConnect = Connect();
            if (!isConnect)
            {
                MessageBox.Show("Failed to connect to serial port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_Serial != null && _Serial.IsOpen)
            {
                _Serial.Close();
            }
        }

        // カメラ画像を描画する関数
        private async void ShotImage()
        {
            var camera = new VideoCapture(0);
            while (true)
            {
                using (var frame = new Mat())
                {
                    camera.Read(frame);
                    if (!frame.Empty())
                    {
                        await UpdateUI(frame.ToBitmap(), mouse_picture);
                    }
                }
            }
        }

        // シリアル入力接続を管理する関数
        private bool Connect()
        {
            // シリアルポートの初期化
            _Serial = new SerialPort
            {
                PortName = "COM11",
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = false,
                RtsEnable = false,
                ReadTimeout = 10000,
                WriteTimeout = 10000,
            };
            // データが入力された際のイベント
            _Serial.DataReceived += Serial_DataReceived;

            try
            {
                // ポートの監視を開始する
                _Serial.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        // シリアル入力からデータを読み取って配列に保存する関数
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (_Serial.BytesToRead > 0)
                {
                    string data = _Serial.ReadLine();
                    string[] values = data.Split(',');
                    if (values.Length == 4)
                    {
                        int button = int.Parse(values[0]);
                        float accX = float.Parse(values[1]);
                        float accY = float.Parse(values[2]);
                        float accZ = float.Parse(values[3]);

                        // カーソル位置を取得
                        System.Drawing.Point cursorPos = Cursor.Position;
                        System.Drawing.Point picturePos = mouse_picture.PointToClient(cursorPos);

                        // ボタンAが押されている場合、描画を行う
                        if (button == 1 &&
                            picturePos.X >= 0 && picturePos.X < mouse_picture.Width &&
                            picturePos.Y >= 0 && picturePos.Y < mouse_picture.Height)
                        {
                            if (!isDrawing)
                            {
                                isDrawing = true;
                                lastPoint = picturePos;
                            }
                            else
                            {
                                DrawLine(lastPoint, picturePos);
                                lastPoint = picturePos;
                            }
                        }
                        else
                        {
                            isDrawing = false;
                        }

                        btnA.Add(button);
                        accXValues.Add(accX);
                        accYValues.Add(accY);
                        accZValues.Add(accZ);

                        if (accXValues.Count > 100)
                        {
                            btnA.RemoveAt(0);
                            accXValues.RemoveAt(0);
                            accYValues.RemoveAt(0);
                            accZValues.RemoveAt(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // グラフを描画する関数
        private async void Timer_Tick(object sender, EventArgs e)
        {
            Bitmap bitmap = new Bitmap(slant_picture.Width, slant_picture.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                Pen penX = new Pen(Color.Red, 2); // x方向の加速度は赤色
                Pen penY = new Pen(Color.Green, 2); // y方向の加速度は緑色
                Pen penZ = new Pen(Color.Blue, 2); // z方向の加速度は青色

                for (int i = 1; i < accXValues.Count; i++)
                {
                    int x1 = (i - 1) * slant_picture.Width / 100;
                    int x2 = i * slant_picture.Width / 100;
                    int y1X = slant_picture.Height / 2 - (int)(accXValues[i - 1] * 100);
                    int y2X = slant_picture.Height / 2 - (int)(accXValues[i] * 100);
                    int y1Y = slant_picture.Height / 2 - (int)(accYValues[i - 1] * 100);
                    int y2Y = slant_picture.Height / 2 - (int)(accYValues[i] * 100);
                    int y1Z = slant_picture.Height / 2 - (int)(accZValues[i - 1] * 100);
                    int y2Z = slant_picture.Height / 2 - (int)(accZValues[i] * 100);

                    g.DrawLine(penX, x1, y1X, x2, y2X);
                    g.DrawLine(penY, x1, y1Y, x2, y2Y);
                    g.DrawLine(penZ, x1, y1Z, x2, y2Z);
                }
            }

            await UpdateUI(bitmap, slant_picture);
        }

        // 描画を行う関数
        private void DrawLine(System.Drawing.Point start, System.Drawing.Point end)
        {
            using (Graphics g = Graphics.FromImage(drawingLayer))
            {
                using (Pen pen = new Pen(Color.Blue, 2))
                {
                    g.DrawLine(pen, start, end);
                }
            }
        }

        // UIを更新する関数
        private async Task UpdateUI(Bitmap bitmap, PictureBox pictureName)
        {
            await Task.Run(() =>
            {
                try {
                    pictureName.Invoke(new Action(() =>
                    {
                        if (pictureName.Image != null)
                        {
                            var oldImage = pictureName.Image;
                            pictureName.Image = bitmap;
                            oldImage.Dispose();
                        }
                        else
                        {
                            pictureName.Image = bitmap;
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }                
            });
        }
    }
}
