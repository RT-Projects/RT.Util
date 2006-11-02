namespace RT.Util.Controls
{
    partial class Separator
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GB = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // GB
            // 
            this.GB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GB.Location = new System.Drawing.Point(-8, 1);
            this.GB.Name = "GB";
            this.GB.Size = new System.Drawing.Size(144, 34);
            this.GB.TabIndex = 6;
            this.GB.TabStop = false;
            this.GB.Text = "Separator";
            // 
            // Separator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GB);
            this.Name = "Separator";
            this.Size = new System.Drawing.Size(120, 16);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox GB;
    }
}
