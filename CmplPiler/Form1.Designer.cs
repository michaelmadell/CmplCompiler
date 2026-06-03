namespace CmplPiler
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnLoad = new Button();
            cmbProfiles = new ComboBox();
            btnBuild = new Button();
            txtOutput = new TextBox();
            SuspendLayout();
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(399, 359);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(133, 34);
            btnLoad.TabIndex = 0;
            btnLoad.Text = "Select .cmpl file...";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // cmbProfiles
            // 
            cmbProfiles.FormattingEnabled = true;
            cmbProfiles.Location = new Point(12, 366);
            cmbProfiles.Name = "cmbProfiles";
            cmbProfiles.Size = new Size(272, 23);
            cmbProfiles.TabIndex = 1;
            // 
            // btnBuild
            // 
            btnBuild.Location = new Point(538, 359);
            btnBuild.Name = "btnBuild";
            btnBuild.Size = new Size(133, 34);
            btnBuild.TabIndex = 2;
            btnBuild.Text = "Build Project";
            btnBuild.UseVisualStyleBackColor = true;
            btnBuild.Click += btnBuild_Click;
            // 
            // txtOutput
            // 
            txtOutput.Location = new Point(12, 12);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(659, 341);
            txtOutput.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(683, 405);
            Controls.Add(txtOutput);
            Controls.Add(btnBuild);
            Controls.Add(cmbProfiles);
            Controls.Add(btnLoad);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnLoad;
        private ComboBox cmbProfiles;
        private Button btnBuild;
        private TextBox txtOutput;
    }
}
