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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblInterrupt = new System.Windows.Forms.Label();
            this.lblRealChipSenderIsRunning = new System.Windows.Forms.Label();
            this.lblEmuChipSenderIsRunning = new System.Windows.Forms.Label();
            this.lblDebug = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.lblDataSenderIsRunning = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.lblDataMakerIsRunning = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.lblRealChipSenderBufferSize = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lblEmuChipSenderBufferSize = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.lblDataSenderBufferSize = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblDataSenderBufferCounter = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblEmuSeqCounter = new System.Windows.Forms.Label();
            this.lblDriverSeqCounter = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.lblSeqCounter = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
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
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.lblInterrupt);
            this.groupBox1.Controls.Add(this.lblRealChipSenderIsRunning);
            this.groupBox1.Controls.Add(this.lblEmuChipSenderIsRunning);
            this.groupBox1.Controls.Add(this.lblDebug);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.lblDataSenderIsRunning);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.lblDataMakerIsRunning);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.lblRealChipSenderBufferSize);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.lblEmuChipSenderBufferSize);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.lblDataSenderBufferSize);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.lblDataSenderBufferCounter);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.lblEmuSeqCounter);
            this.groupBox1.Controls.Add(this.lblDriverSeqCounter);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.lblSeqCounter);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(12, 81);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(310, 218);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Status";
            // 
            // lblInterrupt
            // 
            this.lblInterrupt.Location = new System.Drawing.Point(209, 174);
            this.lblInterrupt.Name = "lblInterrupt";
            this.lblInterrupt.Size = new System.Drawing.Size(95, 12);
            this.lblInterrupt.TabIndex = 0;
            this.lblInterrupt.Text = "Disable";
            this.lblInterrupt.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRealChipSenderIsRunning
            // 
            this.lblRealChipSenderIsRunning.Location = new System.Drawing.Point(209, 151);
            this.lblRealChipSenderIsRunning.Name = "lblRealChipSenderIsRunning";
            this.lblRealChipSenderIsRunning.Size = new System.Drawing.Size(95, 12);
            this.lblRealChipSenderIsRunning.TabIndex = 0;
            this.lblRealChipSenderIsRunning.Text = "Stop";
            this.lblRealChipSenderIsRunning.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblEmuChipSenderIsRunning
            // 
            this.lblEmuChipSenderIsRunning.Location = new System.Drawing.Point(209, 139);
            this.lblEmuChipSenderIsRunning.Name = "lblEmuChipSenderIsRunning";
            this.lblEmuChipSenderIsRunning.Size = new System.Drawing.Size(95, 12);
            this.lblEmuChipSenderIsRunning.TabIndex = 0;
            this.lblEmuChipSenderIsRunning.Text = "Stop";
            this.lblEmuChipSenderIsRunning.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblDebug
            // 
            this.lblDebug.AutoSize = true;
            this.lblDebug.Location = new System.Drawing.Point(6, 192);
            this.lblDebug.Name = "lblDebug";
            this.lblDebug.Size = new System.Drawing.Size(35, 12);
            this.lblDebug.TabIndex = 0;
            this.lblDebug.Text = "debug";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 174);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(54, 12);
            this.label14.TabIndex = 0;
            this.label14.Text = "Interrupt :";
            // 
            // lblDataSenderIsRunning
            // 
            this.lblDataSenderIsRunning.Location = new System.Drawing.Point(209, 127);
            this.lblDataSenderIsRunning.Name = "lblDataSenderIsRunning";
            this.lblDataSenderIsRunning.Size = new System.Drawing.Size(95, 12);
            this.lblDataSenderIsRunning.TabIndex = 0;
            this.lblDataSenderIsRunning.Text = "Stop";
            this.lblDataSenderIsRunning.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 151);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(142, 12);
            this.label13.TabIndex = 0;
            this.label13.Text = "RealChipSenderIsRunning :";
            // 
            // lblDataMakerIsRunning
            // 
            this.lblDataMakerIsRunning.Location = new System.Drawing.Point(209, 115);
            this.lblDataMakerIsRunning.Name = "lblDataMakerIsRunning";
            this.lblDataMakerIsRunning.Size = new System.Drawing.Size(95, 12);
            this.lblDataMakerIsRunning.TabIndex = 0;
            this.lblDataMakerIsRunning.Text = "Stop";
            this.lblDataMakerIsRunning.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 139);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(141, 12);
            this.label10.TabIndex = 0;
            this.label10.Text = "EmuChipSenderIsRunning :";
            // 
            // lblRealChipSenderBufferSize
            // 
            this.lblRealChipSenderBufferSize.Location = new System.Drawing.Point(209, 95);
            this.lblRealChipSenderBufferSize.Name = "lblRealChipSenderBufferSize";
            this.lblRealChipSenderBufferSize.Size = new System.Drawing.Size(95, 12);
            this.lblRealChipSenderBufferSize.TabIndex = 0;
            this.lblRealChipSenderBufferSize.Text = "0";
            this.lblRealChipSenderBufferSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 127);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(120, 12);
            this.label11.TabIndex = 0;
            this.label11.Text = "DataSenderIsRunning :";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 115);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(116, 12);
            this.label8.TabIndex = 0;
            this.label8.Text = "DataMakerIsRunning :";
            // 
            // lblEmuChipSenderBufferSize
            // 
            this.lblEmuChipSenderBufferSize.Location = new System.Drawing.Point(209, 83);
            this.lblEmuChipSenderBufferSize.Name = "lblEmuChipSenderBufferSize";
            this.lblEmuChipSenderBufferSize.Size = new System.Drawing.Size(95, 12);
            this.lblEmuChipSenderBufferSize.TabIndex = 0;
            this.lblEmuChipSenderBufferSize.Text = "0";
            this.lblEmuChipSenderBufferSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 95);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(145, 12);
            this.label9.TabIndex = 0;
            this.label9.Text = "RealChipSenderBufferSize :";
            // 
            // lblDataSenderBufferSize
            // 
            this.lblDataSenderBufferSize.Location = new System.Drawing.Point(209, 71);
            this.lblDataSenderBufferSize.Name = "lblDataSenderBufferSize";
            this.lblDataSenderBufferSize.Size = new System.Drawing.Size(95, 12);
            this.lblDataSenderBufferSize.TabIndex = 0;
            this.lblDataSenderBufferSize.Text = "0";
            this.lblDataSenderBufferSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 83);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(144, 12);
            this.label7.TabIndex = 0;
            this.label7.Text = "EmuChipSenderBufferSize :";
            // 
            // lblDataSenderBufferCounter
            // 
            this.lblDataSenderBufferCounter.Location = new System.Drawing.Point(209, 51);
            this.lblDataSenderBufferCounter.Name = "lblDataSenderBufferCounter";
            this.lblDataSenderBufferCounter.Size = new System.Drawing.Size(95, 12);
            this.lblDataSenderBufferCounter.TabIndex = 0;
            this.lblDataSenderBufferCounter.Text = "0";
            this.lblDataSenderBufferCounter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 71);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(123, 12);
            this.label6.TabIndex = 0;
            this.label6.Text = "DataSenderBufferSize :";
            // 
            // lblEmuSeqCounter
            // 
            this.lblEmuSeqCounter.Location = new System.Drawing.Point(209, 27);
            this.lblEmuSeqCounter.Name = "lblEmuSeqCounter";
            this.lblEmuSeqCounter.Size = new System.Drawing.Size(95, 12);
            this.lblEmuSeqCounter.TabIndex = 0;
            this.lblEmuSeqCounter.Text = "0";
            this.lblEmuSeqCounter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblDriverSeqCounter
            // 
            this.lblDriverSeqCounter.Location = new System.Drawing.Point(209, 15);
            this.lblDriverSeqCounter.Name = "lblDriverSeqCounter";
            this.lblDriverSeqCounter.Size = new System.Drawing.Size(95, 12);
            this.lblDriverSeqCounter.TabIndex = 0;
            this.lblDriverSeqCounter.Text = "0";
            this.lblDriverSeqCounter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 27);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(92, 12);
            this.label15.TabIndex = 0;
            this.label15.Text = "EmuSeqCounter :";
            // 
            // lblSeqCounter
            // 
            this.lblSeqCounter.Location = new System.Drawing.Point(209, 39);
            this.lblSeqCounter.Name = "lblSeqCounter";
            this.lblSeqCounter.Size = new System.Drawing.Size(95, 12);
            this.lblSeqCounter.TabIndex = 0;
            this.lblSeqCounter.Text = "0";
            this.lblSeqCounter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 15);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(101, 12);
            this.label12.TabIndex = 0;
            this.label12.Text = "DriverSeqCounter :";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 51);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(142, 12);
            this.label5.TabIndex = 0;
            this.label5.Text = "DataSenderBufferCounter :";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 39);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 12);
            this.label4.TabIndex = 0;
            this.label4.Text = "SeqCounter :";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 311);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.btnRef);
            this.Controls.Add(this.tbFile);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FrmMain";
            this.Text = "TestPlayer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmMain_FormClosed);
            this.Shown += new System.EventHandler(this.FrmMain_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
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
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblSeqCounter;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblDataSenderBufferCounter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblDataSenderBufferSize;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblRealChipSenderBufferSize;
        private System.Windows.Forms.Label lblEmuChipSenderBufferSize;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblDataSenderIsRunning;
        private System.Windows.Forms.Label lblDataMakerIsRunning;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label lblRealChipSenderIsRunning;
        private System.Windows.Forms.Label lblEmuChipSenderIsRunning;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lblEmuSeqCounter;
        private System.Windows.Forms.Label lblDriverSeqCounter;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label lblInterrupt;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label lblDebug;
    }
}

