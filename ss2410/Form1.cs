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

        // 既存のフィールドに追加
        private const int MARKER_SIZE = 20;
        private System.Drawing.Point mousePoint;
        private float scrollSpeed = 1.0f; // デフォルトのスクロールスピード
        private Mat frame;

        // ダブルクリック検出用の変数
        private const int DOUBLE_CLICK_INTERVAL = 800; // ミリ秒
        private DateTime lastButtonPress = DateTime.MinValue;
        private bool isMouseControlEnabled = false;

        // Win32 APIのマウス制御用
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            this.Text = "Aerial mouse GUI";
            this.Icon = new Icon("C:\\Users\\takos\\source\\repos\\ss2410\\ss2410\\endo.ico");

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

            // マウスポイントの初期化
            mousePoint = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2,
                                 Screen.PrimaryScreen.Bounds.Height / 2);

            // タイマーのTickイベントにマウス更新を追加
            timer.Tick += (s, e) => {
                if (isMouseControlEnabled)
                {
                    UpdateMousePoint();
                }
            };
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
                // シリアルバッファーにデータがある限り読み続ける
                while (_Serial.BytesToRead > 0)
                {
                    string data = _Serial.ReadLine();
                    string[] values = data.Split(',');
                    if (values.Length == 4)
                    {
                        int buttonA = int.Parse(values[0]);
                        float accX = float.Parse(values[1]);
                        float accY = float.Parse(values[2]);
                        float accZ = float.Parse(values[3]);

                        ProcessButtonA(buttonA);

                        btnA.Add(buttonA);
                        accXValues.Add(accX);
                        accYValues.Add(accY);
                        accZValues.Add(accZ);

                        if (accXValues.Count > 100)
                        {
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

        // マウスポイントを更新する関数
        private void UpdateMousePoint()
        {
            try
            {
                // 直近5個の加速度データを取得
                var lastFiveX = accXValues.Skip(Math.Max(0, accXValues.Count - 5)).ToList();
                var lastFiveY = accYValues.Skip(Math.Max(0, accYValues.Count - 5)).ToList();
                var lastFiveZ = accZValues.Skip(Math.Max(0, accZValues.Count - 5)).ToList();

                if (lastFiveX.Count > 0)
                {
                    // 平均値を計算
                    float averageX = lastFiveX.Average();
                    float averageY = lastFiveY.Average();

                    // スクロールスピードに応じた変位を加算
                    mousePoint.X += (int)(scrollSpeed * averageX * 10);
                    mousePoint.Y -= (int)(scrollSpeed * averageY * 10);

                    // 画面の境界チェック
                    mousePoint.X = Math.Max(0, Math.Min(mousePoint.X,
                        Screen.PrimaryScreen.Bounds.Width - MARKER_SIZE));
                    mousePoint.Y = Math.Max(0, Math.Min(mousePoint.Y,
                        Screen.PrimaryScreen.Bounds.Height - MARKER_SIZE));

                    // カーソル位置を更新
                    SetCursorPos(mousePoint.X, mousePoint.Y);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Mouse update error: {ex.Message}");
            }
        }

        // ボタンAのダブルクリック検出とマウス制御の切り替え
        private void ProcessButtonA(int buttonState)
        {
            if (buttonState == 1) // ボタンが押された時
            {
                DateTime now = DateTime.Now;
                TimeSpan timeSinceLastPress = now - lastButtonPress;

                if (timeSinceLastPress.TotalMilliseconds <= DOUBLE_CLICK_INTERVAL)
                {
                    // ダブルクリック検出
                    isMouseControlEnabled = !isMouseControlEnabled;

                    // UI更新（状態を表示）
                    this.Invoke(new Action(() =>
                    {
                        string status = isMouseControlEnabled ? "有効" : "無効";
                        this.Text = $"Aerial mouse GUI - マウス制御: {status}";
                        if (isMouseControlEnabled == true)
                        {
                            label1.Text = "空中マウスが有効，マイコンをクリックしながら線を描画できます．ダブルクリックで空中マウス解除";
                        }
                        else
                        {
                            label1.Text = "マイコンのダブルクリックで空中マウスを起動出来ます";
                        }
                    }));
                }

                lastButtonPress = now;
            }
        }

        // スクロールスピードを設定するメソッド
        public void SetScrollSpeed(float speed)
        {
            if (speed >= 0.1f && speed <= 5.0f)
            {
                scrollSpeed = speed;
            }
        }

        // マウス位置を取得するメソッド
        public System.Drawing.Point GetMousePosition()
        {
            return mousePoint;
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
