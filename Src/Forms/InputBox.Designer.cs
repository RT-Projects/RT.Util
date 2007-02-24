namespace RT.Util.Dialogs
{
    partial class InputBox
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
            this.PromptLabel = new System.Windows.Forms.Label();
            this.EnterBox = new System.Windows.Forms.TextBox();
            this.BtnOK = new System.Windows.Forms.Button();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.LayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // PromptLabel
            // 
            this.PromptLabel.AutoSize = true;
            this.LayoutPanel.SetColumnSpan(this.PromptLabel, 3);
            this.PromptLabel.Location = new System.Drawing.Point(15, 16);
            this.PromptLabel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.PromptLabel.Name = "PromptLabel";
            this.PromptLabel.Size = new System.Drawing.Size(109, 16);
            this.PromptLabel.TabIndex = 0;
            this.PromptLabel.Text = "Please enter text:";
            // 
            // EnterBox
            // 
            this.EnterBox.AcceptsReturn = true;
            this.EnterBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.LayoutPanel.SetColumnSpan(this.EnterBox, 3);
            this.EnterBox.Location = new System.Drawing.Point(15, 44);
            this.EnterBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.EnterBox.Name = "EnterBox";
            this.EnterBox.Size = new System.Drawing.Size(463, 23);
            this.EnterBox.TabIndex = 1;
            // 
            // BtnOK
            // 
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.AutoSize = true;
            this.BtnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.BtnOK.Location = new System.Drawing.Point(171, 79);
            this.BtnOK.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(144, 28);
            this.BtnOK.TabIndex = 2;
            this.BtnOK.Text = "OK";
            this.BtnOK.UseVisualStyleBackColor = true;
            // 
            // BtnCancel
            // 
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.AutoSize = true;
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(327, 79);
            this.BtnCancel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(151, 28);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.UseVisualStyleBackColor = true;
            // 
            // LayoutPanel
            // 
            this.LayoutPanel.AutoSize = true;
            this.LayoutPanel.ColumnCount = 3;
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.LayoutPanel.Controls.Add(this.PromptLabel, 0, 0);
            this.LayoutPanel.Controls.Add(this.BtnCancel, 2, 2);
            this.LayoutPanel.Controls.Add(this.EnterBox, 0, 1);
            this.LayoutPanel.Controls.Add(this.BtnOK, 1, 2);
            this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LayoutPanel.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.LayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.LayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.LayoutPanel.Name = "LayoutPanel";
            this.LayoutPanel.Padding = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this.LayoutPanel.RowCount = 3;
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.LayoutPanel.Size = new System.Drawing.Size(493, 116);
            this.LayoutPanel.TabIndex = 4;
            // 
            // InputBox
            // 
            this.AcceptButton = this.BtnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(493, 116);
            this.Controls.Add(this.LayoutPanel);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputBox";
            this.Text = "InputBox";
            this.LayoutPanel.ResumeLayout(false);
            this.LayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label PromptLabel;
        private System.Windows.Forms.TextBox EnterBox;
        private System.Windows.Forms.Button BtnOK;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.TableLayoutPanel LayoutPanel;
    }
}