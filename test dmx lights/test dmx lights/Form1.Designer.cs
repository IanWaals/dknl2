namespace DMXControl
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TrackBar trbChannel1iwaa;
        private System.Windows.Forms.Label lblChannel1Value;
        private System.Windows.Forms.Label lblTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.trbChannel1iwaa = new System.Windows.Forms.TrackBar();
            this.lblChannel1Value = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblChannel2Value = new System.Windows.Forms.Label();
            this.trbChannel2iwaa = new System.Windows.Forms.TrackBar();
            this.lblChannel3Value = new System.Windows.Forms.Label();
            this.trbChannel3iwaa = new System.Windows.Forms.TrackBar();
            this.btnTogglePowerIwaa = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trbChannel1iwaa)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbChannel2iwaa)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbChannel3iwaa)).BeginInit();
            this.SuspendLayout();
            // 
            // trbChannel1iwaa
            // 
            this.trbChannel1iwaa.Location = new System.Drawing.Point(30, 40);
            this.trbChannel1iwaa.Maximum = 255;
            this.trbChannel1iwaa.Name = "trbChannel1iwaa";
            this.trbChannel1iwaa.Size = new System.Drawing.Size(300, 56);
            this.trbChannel1iwaa.TabIndex = 0;
            this.trbChannel1iwaa.TickFrequency = 25;
            this.trbChannel1iwaa.Scroll += new System.EventHandler(this.trbChannel1Iwaa_Scroll);
            // 
            // lblChannel1Value
            // 
            this.lblChannel1Value.AutoSize = true;
            this.lblChannel1Value.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.lblChannel1Value.Location = new System.Drawing.Point(30, 85);
            this.lblChannel1Value.Name = "lblChannel1Value";
            this.lblChannel1Value.Size = new System.Drawing.Size(90, 18);
            this.lblChannel1Value.TabIndex = 1;
            this.lblChannel1Value.Text = "Channel 1: 0";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(30, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(140, 20);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "DMX Channel 1";
            // 
            // lblChannel2Value
            // 
            this.lblChannel2Value.AutoSize = true;
            this.lblChannel2Value.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.lblChannel2Value.Location = new System.Drawing.Point(30, 174);
            this.lblChannel2Value.Name = "lblChannel2Value";
            this.lblChannel2Value.Size = new System.Drawing.Size(90, 18);
            this.lblChannel2Value.TabIndex = 4;
            this.lblChannel2Value.Text = "Channel 2: 0";
            // 
            // trbChannel2iwaa
            // 
            this.trbChannel2iwaa.Location = new System.Drawing.Point(30, 129);
            this.trbChannel2iwaa.Maximum = 255;
            this.trbChannel2iwaa.Name = "trbChannel2iwaa";
            this.trbChannel2iwaa.Size = new System.Drawing.Size(300, 56);
            this.trbChannel2iwaa.TabIndex = 3;
            this.trbChannel2iwaa.TickFrequency = 25;
            this.trbChannel2iwaa.Scroll += new System.EventHandler(this.trbChannel2_Scroll);
            // 
            // lblChannel3Value
            // 
            this.lblChannel3Value.AutoSize = true;
            this.lblChannel3Value.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.lblChannel3Value.Location = new System.Drawing.Point(34, 259);
            this.lblChannel3Value.Name = "lblChannel3Value";
            this.lblChannel3Value.Size = new System.Drawing.Size(90, 18);
            this.lblChannel3Value.TabIndex = 6;
            this.lblChannel3Value.Text = "Channel 3: 0";
            // 
            // trbChannel3iwaa
            // 
            this.trbChannel3iwaa.Location = new System.Drawing.Point(34, 214);
            this.trbChannel3iwaa.Maximum = 255;
            this.trbChannel3iwaa.Name = "trbChannel3iwaa";
            this.trbChannel3iwaa.Size = new System.Drawing.Size(300, 56);
            this.trbChannel3iwaa.TabIndex = 5;
            this.trbChannel3iwaa.TickFrequency = 25;
            this.trbChannel3iwaa.Scroll += new System.EventHandler(this.trbChannel3iwaa_Scroll);
            // 
            // btnTogglePowerIwaa
            // 
            this.btnTogglePowerIwaa.Location = new System.Drawing.Point(30, 315);
            this.btnTogglePowerIwaa.Name = "btnTogglePowerIwaa";
            this.btnTogglePowerIwaa.Size = new System.Drawing.Size(300, 23);
            this.btnTogglePowerIwaa.TabIndex = 7;
            this.btnTogglePowerIwaa.Text = "on/off";
            this.btnTogglePowerIwaa.UseVisualStyleBackColor = true;
            this.btnTogglePowerIwaa.Click += new System.EventHandler(this.btnTogglePowerIwaa_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(380, 450);
            this.Controls.Add(this.btnTogglePowerIwaa);
            this.Controls.Add(this.lblChannel3Value);
            this.Controls.Add(this.trbChannel3iwaa);
            this.Controls.Add(this.lblChannel2Value);
            this.Controls.Add(this.trbChannel2iwaa);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblChannel1Value);
            this.Controls.Add(this.trbChannel1iwaa);
            this.Name = "Form1";
            this.Text = "DMX Control - COM12";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trbChannel1iwaa)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbChannel2iwaa)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbChannel3iwaa)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblChannel2Value;
        private System.Windows.Forms.TrackBar trbChannel2iwaa;
        private System.Windows.Forms.Label lblChannel3Value;
        private System.Windows.Forms.TrackBar trbChannel3iwaa;
        private System.Windows.Forms.Button btnTogglePowerIwaa;
    }
}