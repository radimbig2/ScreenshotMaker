namespace ScreenshotMaker
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.StartWithWindowsCheckBox = new System.Windows.Forms.CheckBox();
            this.CapturePrtScCheckBox = new System.Windows.Forms.CheckBox();
            this.CaptureWinShiftSCheckBox = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // StartWithWindowsCheckBox
            // 
            this.StartWithWindowsCheckBox.AutoSize = true;
            this.StartWithWindowsCheckBox.Location = new System.Drawing.Point(12, 12);
            this.StartWithWindowsCheckBox.Name = "StartWithWindowsCheckBox";
            this.StartWithWindowsCheckBox.Size = new System.Drawing.Size(114, 17);
            this.StartWithWindowsCheckBox.TabIndex = 1;
            this.StartWithWindowsCheckBox.Text = "Start with windows";
            this.StartWithWindowsCheckBox.UseVisualStyleBackColor = true;
            this.StartWithWindowsCheckBox.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // CapturePrtScCheckBox
            // 
            this.CapturePrtScCheckBox.AutoSize = true;
            this.CapturePrtScCheckBox.Location = new System.Drawing.Point(12, 35);
            this.CapturePrtScCheckBox.Name = "CapturePrtScCheckBox";
            this.CapturePrtScCheckBox.Size = new System.Drawing.Size(92, 17);
            this.CapturePrtScCheckBox.TabIndex = 2;
            this.CapturePrtScCheckBox.Text = "Capture PrtSc";
            this.CapturePrtScCheckBox.UseVisualStyleBackColor = true;
            this.CapturePrtScCheckBox.CheckedChanged += new System.EventHandler(this.CapturePrtScCheckBox_CheckedChanged);
            // 
            // CaptureWinShiftSCheckBox
            // 
            this.CaptureWinShiftSCheckBox.AutoSize = true;
            this.CaptureWinShiftSCheckBox.Location = new System.Drawing.Point(12, 58);
            this.CaptureWinShiftSCheckBox.Name = "CaptureWinShiftSCheckBox";
            this.CaptureWinShiftSCheckBox.Size = new System.Drawing.Size(125, 17);
            this.CaptureWinShiftSCheckBox.TabIndex = 3;
            this.CaptureWinShiftSCheckBox.Text = "Capture Win+Shift+S";
            this.CaptureWinShiftSCheckBox.UseVisualStyleBackColor = true;
            this.CaptureWinShiftSCheckBox.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 81);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(139, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Hide to tray";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(382, 35);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(475, 460);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(620, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(13, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "0";
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "notifyIcon1";
            this.notifyIcon.Visible = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(682, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "0Mb";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(174, 81);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(111, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Open the folder";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(869, 509);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.CaptureWinShiftSCheckBox);
            this.Controls.Add(this.CapturePrtScCheckBox);
            this.Controls.Add(this.StartWithWindowsCheckBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Screenshot catcher";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox StartWithWindowsCheckBox;
        private System.Windows.Forms.CheckBox CapturePrtScCheckBox;
        private System.Windows.Forms.CheckBox CaptureWinShiftSCheckBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button2;
    }
}

