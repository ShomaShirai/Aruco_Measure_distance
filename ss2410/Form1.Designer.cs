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
            ((System.ComponentModel.ISupportInitialize)(this.mouse_picture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.slant_picture)).BeginInit();
            this.SuspendLayout();
            // 
            // mouse_picture
            // 
            this.mouse_picture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mouse_picture.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.mouse_picture.Location = new System.Drawing.Point(14, 87);
            this.mouse_picture.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.mouse_picture.Name = "mouse_picture";
            this.mouse_picture.Size = new System.Drawing.Size(600, 476);
            this.mouse_picture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.mouse_picture.TabIndex = 0;
            this.mouse_picture.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("MS UI Gothic", 15F);
            this.label1.Location = new System.Drawing.Point(18, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(647, 30);
            this.label1.TabIndex = 1;
            this.label1.Text = "左画面を見ながら空中マウスを自由に動かしてください";
            // 
            // slant_picture
            // 
            this.slant_picture.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.slant_picture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.slant_picture.Location = new System.Drawing.Point(644, 87);
            this.slant_picture.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.slant_picture.Name = "slant_picture";
            this.slant_picture.Size = new System.Drawing.Size(600, 476);
            this.slant_picture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.slant_picture.TabIndex = 2;
            this.slant_picture.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1333, 675);
            this.Controls.Add(this.slant_picture);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.mouse_picture);
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.mouse_picture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.slant_picture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox mouse_picture;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox slant_picture;
    }
}

