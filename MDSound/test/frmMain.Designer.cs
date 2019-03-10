namespace test
{
    partial class FrmMain
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
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
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.tbFile = new System.Windows.Forms.TextBox();
            this.btnRef = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select VGM";
            // 
            // tbFile
            // 
            this.tbFile.Location = new System.Drawing.Point(12, 25);
            this.tbFile.Name = "tbFile";
            this.tbFile.Size = new System.Drawing.Size(286, 19);
            this.tbFile.TabIndex = 1;
            // 
            // btnRef
            // 
            this.btnRef.Location = new System.Drawing.Point(304, 23);
            this.btnRef.Name = "btnRef";
            this.btnRef.Size = new System.Drawing.Size(25, 23);
            this.btnRef.TabIndex = 2;
            this.btnRef.Text = "...";
            this.btnRef.UseVisualStyleBackColor = true;
            this.btnRef.Click += new System.EventHandler(this.BtnRef_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(173, 52);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(75, 23);
            this.btnPlay.TabIndex = 3;
            this.btnPlay.Text = ">";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.BtnPlay_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(254, 52);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 4;
            this.btnStop.Text = "[]";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.BtnStop_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(-174, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(179, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "||||||||||||||||||||||||||||||||||||||||||||||||||||||||||";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 10;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(-174, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(179, 12);
            this.label3.TabIndex = 5;
            this.label3.Text = "||||||||||||||||||||||||||||||||||||||||||||||||||||||||||";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 82);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.btnRef);
            this.Controls.Add(this.tbFile);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "frmMain";
            this.Text = "TestPlayer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmMain_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbFile;
        private System.Windows.Forms.Button btnRef;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label3;
    }
}

