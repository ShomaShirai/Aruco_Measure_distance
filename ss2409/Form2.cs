using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ss2409
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            // ComboBoxにカメラの種類を追加
            comboBox1.Items.Add("USBカメラ");
            comboBox1.Items.Add("Windows Virtual Camera");
            comboBox1.Items.Add("Dummyカメラ");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string selectedCamera = comboBox1.SelectedItem?.ToString();
            if (selectedCamera == null)
            {
                label2.Text = "カメラを選択してください";
                return;
            }

            switch (selectedCamera)
            {
                case "USBカメラ":
                    StartUSBCamera();
                    break;
                case "Windows Virtual Camera":
                    StartVirtualCamera();
                    break;
                case "Dummyカメラ":
                    label2.Text = "Dummyを開けません";
                    break;
                default:
                    label2.Text = "不明なカメラタイプです";
                    break;
            }
        }

        private void StartUSBCamera()
        {
            // USBカメラを起動するコードをここに追加
            label2.Text = "USBカメラを起動しました";
        }

        private void StartVirtualCamera()
        {
            // Windows Virtual Cameraを起動するコードをここに追加
            label2.Text = "Windows Virtual Cameraを起動しました";
        }
    }
}
