using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        // シリアル通信のパラメータ
        private SerialPort _Serial;
        private List<int> btnA = new List<int>();
        private List<float> accXValues = new List<float>();
        private List<float> accYValues = new List<float>();
        private List<float> accZValues = new List<float>();
        private Timer timer;

        // マウス制御のパラメータ
        private const int MARKER_SIZE = 20; // マウスポインタのサイズ
        private System.Drawing.Point mousePoint; // マウスポインタの位置
        private float scrollSpeed = 1.5f; // デフォルトのスクロールスピード

        // 線描画のパラメータ
        private List<System.Drawing.Point> drawingPoints = new List<System.Drawing.Point>(); // 描画する点のリスト
        private bool isDrawing = false; // 描画中かどうかのフラグ
        private Bitmap drawingLayer; // 描画用のbitmao

        // 空中マウスを利用できるか判断するフラグ
        private bool isMouseControlEnabled = false;

        // Win32 APIのマウス制御用
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        // fpsやHzの計測用パラメータ
        private int cameraFrameCount = 0;
        private int pictureBoxFrameCount = 0;
        private int serialDataCount = 0;
        private Stopwatch stopwatch = new Stopwatch();

        // ボート番号とボーレートを受け取るパラメータ
        private string portNameFromCombobox;
        private int baudRateFromComboBox;

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
            this.Text = "Aerial Mouse GUI";
            this.Icon = new Icon("C:\\Users\\takos\\source\\repos\\ss2410\\ss2410\\endo.ico");    

            this.Resize += Form1_Resize;

            // キーボードのqを押すことでウィンドウを閉じれるようにする
            this.KeyDown += Form1_KeyDown;

            // グラフを描画するタイマーを初期化
            timer = new Timer { Interval = 100 }; // 0.1秒ごとにグラフを更新
            timer.Tick += Timer_Tick;
            timer.Start();

            // FPS計測用のタイマーを設定（1秒ごとに更新）
            stopwatch.Start();

            // FPS計測用のタイマーを設定（1秒ごとに更新）
            Timer fpsTimer = new Timer { Interval = 1000 };
            fpsTimer.Tick += FpsTimer_Tick;
            fpsTimer.Start();

            // カメラを開始する
            Task.Run(() => ShotImage());

            // マウスポイントの初期化
            mousePoint = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2);

            // 空中マウス制御の更新イベントを設定
            timer.Tick += (s, e) =>
            {
                if (isMouseControlEnabled) UpdateMousePoint();
            };

            InitializeDrawingLayer();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            SetScrollSpeed(trackBar1.Value / 10.0f);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_Serial != null)
            {
                if (_Serial.IsOpen)
                {
                    if (isMouseControlEnabled)
                    {
                        // 空中マウスがONの場合
                        MessageBox.Show("空中マウスをOFFにしてからシリアル通信を切ってください", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        // シリアル通信をOFFにする
                        _Serial.Close();
                        button3.Text = "シリアル通信ON";
                        label1.Text = "シリアル通信が切られました．[シリアル通信ON]を押して通信を開始できます";
                        return;
                    }
                }
                else
                {
                    // シリアル通信をONにする
                    try
                    {
                        bool isConnect = Connect();
                        if (!isConnect)
                        {
                            MessageBox.Show("Failed to connect to serial port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        button3.Text = "シリアル通信OFF";
                        label1.Text = "シリアル通信中です．空中マウスをONにできます";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"シリアルポートの接続に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                // _Serialが初期化されていない場合の処理
                bool isConnect = Connect();
                if (!isConnect)
                {
                    MessageBox.Show("Failed to connect to serial port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                button3.Text = "シリアル通信OFF";
                label1.Text = "シリアル通信中です．空中マウスをONにできます";
            }
        }


        private void Form1_Resize(object sender, EventArgs e)
        {
            // フォームの中央を計算
            int centerX = this.ClientSize.Width / 2;

            // PictureBox の幅を取得
            int pictureWidth = mouse_picture.Width;

            // mouse_picture の位置を設定（中央より左側）
            mouse_picture.Location = new System.Drawing.Point(centerX - pictureWidth - 6, mouse_picture.Location.Y);

            // slant_picture の位置を設定（中央より右側）
            slant_picture.Location = new System.Drawing.Point(centerX + 6, slant_picture.Location.Y);

            // PictureBox の幅を設定
            mouse_picture.Width = slant_picture.Width = (this.ClientSize.Width - 100) / 2;
        }


        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            // 経過時間を取得
            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            // 各値の計算（小数点以下2桁）
            double cameraFps = cameraFrameCount / elapsedSeconds;
            double pictureBoxFps = pictureBoxFrameCount / elapsedSeconds;
            double serialHz = serialDataCount / elapsedSeconds;

            // ラベルの更新
            label6.Text = $"カメラ : {cameraFps:F2}FPS";
            label5.Text = $"表示 : {pictureBoxFps:F2}FPS";
            label7.Text = $"センサ : {serialHz:F2}Hz";

            // カウントのリセット
            cameraFrameCount = 0;
            pictureBoxFrameCount = 0;
            serialDataCount = 0;

            // Stopwatchをリセット
            stopwatch.Restart();
        }

        private void InitializeDrawingLayer()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(InitializeDrawingLayer));
                return;
            }

            drawingLayer = new Bitmap(mouse_picture.Width, mouse_picture.Height);
            using (Graphics g = Graphics.FromImage(drawingLayer))
            {
                g.Clear(Color.Transparent);
            }

            mouse_picture.Paint += PaintLine; // PaintLineをPaintイベントに追加
        }

        private void PaintLine(object sender, PaintEventArgs e)
        {
            if (drawingPoints.Count > 1)
            {
                using (Pen pen = new Pen(Color.Blue, 2))
                {
                    // 描画する線を PictureBox に描画
                    e.Graphics.DrawLines(pen, drawingPoints.ToArray());
                }
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_Serial != null && _Serial.IsOpen)
            {
                _Serial.Close();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Q)
            {
                this.Close();
            }
        }

        // カメラ画像を取得
        private async void ShotImage()
        {
            using (var camera = new VideoCapture(0))
            {
                while (true)
                {
                    using (var frame = new Mat())
                    {
                        camera.Read(frame);
                        if (!frame.Empty())
                        {
                            cameraFrameCount++;

                            var bitmap = frame.ToBitmap();
                            await UpdateUI(bitmap, mouse_picture);
                            pictureBoxFrameCount++;
                        };
                    }
                }
            }
        }


        // シリアル通信の設定と接続
        private bool Connect()
        {
            // ポート番号とボーレートをComboBoxから取得
            portNameFromCombobox = comboBox1.SelectedItem?.ToString();
            if (int.TryParse(comboBox2.SelectedItem?.ToString(), out int bandRateFromComboBox))
            {
                baudRateFromComboBox = bandRateFromComboBox;
            }

            _Serial = new SerialPort
            {
                PortName = portNameFromCombobox,
                BaudRate = baudRateFromComboBox,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = false,
                RtsEnable = false,
                ReadTimeout = 10000,
                WriteTimeout = 10000
            };
            _Serial.DataReceived += Serial_DataReceived;

            try
            {
                _Serial.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (_Serial.BytesToRead > 0)
                {
                    string data = _Serial.ReadLine();
                    serialDataCount++;

                    string[] values = data.Split(',');
                    if (values.Length == 4)
                    {
                        int buttonA = int.Parse(values[0]);
                        float accX = float.Parse(values[1]);
                        float accY = float.Parse(values[2]);
                        float accZ = float.Parse(values[3]);

                        ProcessDrawing(buttonA);

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


        private void ProcessDrawing(int buttonState)
        {
            if (!isMouseControlEnabled) return;

            if (InvokeRequired)
            {
                Invoke(new Action<int>(ProcessDrawing), buttonState);
                return;
            }

            if (buttonState == 1)
            {
                System.Drawing.Point currentPoint = GetMousePositionRelativeToControl(mouse_picture);

                if (IsPointInPictureBox(currentPoint))
                {
                    if (!isDrawing)
                    {
                        isDrawing = true;
                        drawingPoints.Clear();
                    }
                    drawingPoints.Add(currentPoint);
                    mouse_picture.Invalidate(); // PaintLineをトリガー
                }
            }
            else
            {
                isDrawing = false;
            }
        }

        private System.Drawing.Point GetMousePositionRelativeToControl(PictureBox control)
        {
            System.Drawing.Point screenPoint = GetMousePosition();
            System.Drawing.Point clientPoint = control.PointToClient(screenPoint);
            return clientPoint;
        }

        private bool IsPointInPictureBox(System.Drawing.Point point)
        {
            return point.X >= 0 && point.X < mouse_picture.Width &&
                   point.Y >= 0 && point.Y < mouse_picture.Height;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            Bitmap bitmap = new Bitmap(slant_picture.Width, slant_picture.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);

                Pen penX = new Pen(Color.Red, 2);
                Pen penY = new Pen(Color.Green, 2);
                Pen penZ = new Pen(Color.Blue, 2);

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

        private void UpdateMousePoint()
        {
            try
            {
                var lastFiveX = accXValues.Skip(Math.Max(0, accXValues.Count - 5)).ToList();
                var lastFiveY = accYValues.Skip(Math.Max(0, accYValues.Count - 5)).ToList();

                if (lastFiveX.Count > 0)
                {
                    float averageX = lastFiveX.Average();
                    float averageY = lastFiveY.Average();

                    mousePoint.X += (int)(scrollSpeed * averageX * 10);
                    mousePoint.Y -= (int)(scrollSpeed * averageY * 10);

                    mousePoint.X = Math.Max(0, Math.Min(mousePoint.X, Screen.PrimaryScreen.Bounds.Width - MARKER_SIZE));
                    mousePoint.Y = Math.Max(0, Math.Min(mousePoint.Y, Screen.PrimaryScreen.Bounds.Height - MARKER_SIZE));

                    SetCursorPos(mousePoint.X, mousePoint.Y);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Mouse update error: " + ex.Message);
            }
        }

        // ボタンを押した際の空中マウスの有効/無効化
        private void button1_Click(object sender, EventArgs e)
        {
            if (_Serial.IsOpen)
            {
                isMouseControlEnabled = !isMouseControlEnabled;
                Debug.WriteLine($"Mouse control enabled: {isMouseControlEnabled}");

                button1.Text = isMouseControlEnabled ? "空中マウス OFF" : "空中マウス ON";

                string status = isMouseControlEnabled ? "有効" : "無効";
                this.Text = $"Aerial Mouse GUI - マウス制御: {status}";
                label1.Text = isMouseControlEnabled ? "空中マウスが有効中．ボタンを押して無効にできます．カメラ画像に青線を描画できます" : "空中マウスが無効．ボタンを押して有効にできます";

                if (isMouseControlEnabled)
                {
                    mousePoint = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2);
                    SetCursorPos(mousePoint.X, mousePoint.Y);
                }
            }            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            drawingPoints.Clear();
            mouse_picture.Invalidate();
        }

        // スクロールスピードのセッター
        public void SetScrollSpeed(float speed)
        {
            if (speed >= 0.1f && speed <= 5.0f)
            {
                scrollSpeed = speed;
            }
        }

        // マウスポインタの位置のゲッター
        private System.Drawing.Point GetMousePosition()
        {
            return mousePoint;
        }

        // UIスレッドで画像を更新
        private async Task UpdateUI(Bitmap bitmap, PictureBox pictureName)
        {
            await Task.Run(() =>
            {
                try
                {
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

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }
    }
}