namespace ss2410
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.mouse_picture = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.slant_picture = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.mouse_picture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.slant_picture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // mouse_picture
            // 
            this.mouse_picture.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mouse_picture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mouse_picture.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.mouse_picture.Location = new System.Drawing.Point(14, 104);
            this.mouse_picture.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.mouse_picture.Name = "mouse_picture";
            this.mouse_picture.Size = new System.Drawing.Size(600, 450);
            this.mouse_picture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.mouse_picture.TabIndex = 0;
            this.mouse_picture.TabStop = false;
            this.mouse_picture.Paint += new System.Windows.Forms.PaintEventHandler(this.PaintLine);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("MS UI Gothic", 15F);
            this.label1.Location = new System.Drawing.Point(18, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(529, 30);
            this.label1.TabIndex = 1;
            this.label1.Text = "ボタンを押すことで空中マウスを起動できます";
            // 
            // slant_picture
            // 
            this.slant_picture.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.slant_picture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.slant_picture.Location = new System.Drawing.Point(637, 104);
            this.slant_picture.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.slant_picture.Name = "slant_picture";
            this.slant_picture.Size = new System.Drawing.Size(600, 450);
            this.slant_picture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.slant_picture.TabIndex = 2;
            this.slant_picture.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label2.Location = new System.Drawing.Point(246, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 24);
            this.label2.TabIndex = 3;
            this.label2.Text = "カメラ映像";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label3.Location = new System.Drawing.Point(861, 75);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(163, 24);
            this.label3.TabIndex = 4;
            this.label3.Text = "センサー値グラフ";
            // 
            // trackBar1
            // 
            this.trackBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.trackBar1.Location = new System.Drawing.Point(491, 596);
            this.trackBar1.Maximum = 25;
            this.trackBar1.Minimum = 5;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(154, 69);
            this.trackBar1.TabIndex = 16;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trackBar1.Value = 15;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label4.Location = new System.Drawing.Point(511, 565);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(112, 24);
            this.label4.TabIndex = 6;
            this.label4.Text = "マウス感度";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button1.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.button1.Location = new System.Drawing.Point(654, 562);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(217, 42);
            this.button1.TabIndex = 17;
            this.button1.Text = "空中マウス ON";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.button2.Location = new System.Drawing.Point(653, 613);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(217, 42);
            this.button2.TabIndex = 18;
            this.button2.Text = "線削除";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label6.Location = new System.Drawing.Point(915, 564);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 24);
            this.label6.TabIndex = 20;
            this.label6.Text = "カメラFPS";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label5.Location = new System.Drawing.Point(915, 595);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 24);
            this.label5.TabIndex = 21;
            this.label5.Text = "表示FPS";
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label7.Location = new System.Drawing.Point(915, 627);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(94, 24);
            this.label7.TabIndex = 22;
            this.label7.Text = "センサHz";
            this.label7.Click += new System.EventHandler(this.label7_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.comboBox1.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "COM10",
            "COM11"});
            this.comboBox1.Location = new System.Drawing.Point(144, 568);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(134, 32);
            this.comboBox1.TabIndex = 23;
            this.comboBox1.Text = "COM11";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label8.Location = new System.Drawing.Point(19, 572);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(115, 24);
            this.label8.TabIndex = 24;
            this.label8.Text = "ポート番号";
            // 
            // comboBox2
            // 
            this.comboBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.comboBox2.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "9600",
            "19200",
            "38400",
            "115200"});
            this.comboBox2.Location = new System.Drawing.Point(144, 616);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(135, 32);
            this.comboBox2.TabIndex = 25;
            this.comboBox2.Text = "115200";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label9.Location = new System.Drawing.Point(701, 1409);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(105, 24);
            this.label9.TabIndex = 26;
            this.label9.Text = "ボーレート";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.label10.Location = new System.Drawing.Point(19, 618);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(105, 24);
            this.label10.TabIndex = 27;
            this.label10.Text = "ボーレート";
            // 
            // button3
            // 
            this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button3.Font = new System.Drawing.Font("MS UI Gothic", 12F);
            this.button3.Location = new System.Drawing.Point(299, 568);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(167, 79);
            this.button3.TabIndex = 28;
            this.button3.Text = "シリアル通信ON";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1251, 671);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.slant_picture);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.mouse_picture);
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.mouse_picture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.slant_picture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox mouse_picture;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox slant_picture;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button button3;
    }
}