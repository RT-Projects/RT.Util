namespace RT.Util.Dialogs
{
    partial class DlgMessageForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DlgMessageForm));
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.layoutButtons = new System.Windows.Forms.TableLayoutPanel();
            this.Btn2 = new System.Windows.Forms.Button();
            this.Btn3 = new System.Windows.Forms.Button();
            this.Btn0 = new System.Windows.Forms.Button();
            this.Btn1 = new System.Windows.Forms.Button();
            this.layoutMessage = new System.Windows.Forms.TableLayoutPanel();
            this.Message = new System.Windows.Forms.Label();
            this.img = new System.Windows.Forms.PictureBox();
            this.pnlLine = new System.Windows.Forms.Panel();
            this.info = new System.Windows.Forms.PictureBox();
            this.question = new System.Windows.Forms.PictureBox();
            this.warning = new System.Windows.Forms.PictureBox();
            this.error = new System.Windows.Forms.PictureBox();
            this.layoutMain.SuspendLayout();
            this.layoutButtons.SuspendLayout();
            this.layoutMessage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.img)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.info)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.question)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.warning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.error)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.AutoSize = true;
            this.layoutMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutMain.ColumnCount = 1;
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutMain.Controls.Add(this.layoutButtons, 0, 2);
            this.layoutMain.Controls.Add(this.layoutMessage, 0, 0);
            this.layoutMain.Controls.Add(this.pnlLine, 0, 1);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(0, 0);
            this.layoutMain.Margin = new System.Windows.Forms.Padding(0);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.RowCount = 3;
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutMain.Size = new System.Drawing.Size(687, 169);
            this.layoutMain.TabIndex = 14;
            // 
            // layoutButtons
            // 
            this.layoutButtons.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.layoutButtons.AutoSize = true;
            this.layoutButtons.ColumnCount = 4;
            this.layoutButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutButtons.Controls.Add(this.Btn0, 0, 0);
            this.layoutButtons.Controls.Add(this.Btn1, 1, 0);
            this.layoutButtons.Controls.Add(this.Btn2, 2, 0);
            this.layoutButtons.Controls.Add(this.Btn3, 3, 0);
            this.layoutButtons.Location = new System.Drawing.Point(220, 84);
            this.layoutButtons.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.layoutButtons.Name = "layoutButtons";
            this.layoutButtons.RowCount = 1;
            this.layoutButtons.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutButtons.Size = new System.Drawing.Size(464, 68);
            this.layoutButtons.TabIndex = 19;
            // 
            // Btn2
            // 
            this.Btn2.AutoSize = true;
            this.Btn2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn2.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.Btn2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Btn2.Location = new System.Drawing.Point(289, 5);
            this.Btn2.Margin = new System.Windows.Forms.Padding(5);
            this.Btn2.MinimumSize = new System.Drawing.Size(80, 25);
            this.Btn2.Name = "Btn2";
            this.Btn2.Size = new System.Drawing.Size(80, 58);
            this.Btn2.TabIndex = 2;
            this.Btn2.Text = "s";
            this.Btn2.UseVisualStyleBackColor = true;
            this.Btn2.Visible = false;
            // 
            // Btn3
            // 
            this.Btn3.AutoSize = true;
            this.Btn3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn3.DialogResult = System.Windows.Forms.DialogResult.No;
            this.Btn3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Btn3.Location = new System.Drawing.Point(379, 5);
            this.Btn3.Margin = new System.Windows.Forms.Padding(5);
            this.Btn3.MinimumSize = new System.Drawing.Size(80, 25);
            this.Btn3.Name = "Btn3";
            this.Btn3.Size = new System.Drawing.Size(80, 58);
            this.Btn3.TabIndex = 3;
            this.Btn3.Text = "button3";
            this.Btn3.UseVisualStyleBackColor = true;
            this.Btn3.Visible = false;
            // 
            // Btn0
            // 
            this.Btn0.AutoSize = true;
            this.Btn0.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn0.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Btn0.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Btn0.Location = new System.Drawing.Point(5, 5);
            this.Btn0.Margin = new System.Windows.Forms.Padding(5);
            this.Btn0.MinimumSize = new System.Drawing.Size(80, 25);
            this.Btn0.Name = "Btn0";
            this.Btn0.Size = new System.Drawing.Size(126, 58);
            this.Btn0.TabIndex = 0;
            this.Btn0.Text = "Longish button text";
            this.Btn0.UseVisualStyleBackColor = true;
            this.Btn0.Visible = false;
            // 
            // Btn1
            // 
            this.Btn1.AutoSize = true;
            this.Btn1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Btn1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Btn1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Btn1.Location = new System.Drawing.Point(141, 5);
            this.Btn1.Margin = new System.Windows.Forms.Padding(5);
            this.Btn1.MinimumSize = new System.Drawing.Size(80, 25);
            this.Btn1.Name = "Btn1";
            this.Btn1.Size = new System.Drawing.Size(138, 58);
            this.Btn1.TabIndex = 1;
            this.Btn1.Text = "Some more long text\r\nwow multi-line\r\nbutton!";
            this.Btn1.UseVisualStyleBackColor = true;
            this.Btn1.Visible = false;
            // 
            // layoutMessage
            // 
            this.layoutMessage.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.layoutMessage.AutoSize = true;
            this.layoutMessage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layoutMessage.BackColor = System.Drawing.Color.White;
            this.layoutMessage.ColumnCount = 2;
            this.layoutMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutMessage.Controls.Add(this.img, 0, 0);
            this.layoutMessage.Controls.Add(this.Message, 1, 0);
            this.layoutMessage.Location = new System.Drawing.Point(0, 0);
            this.layoutMessage.Margin = new System.Windows.Forms.Padding(0);
            this.layoutMessage.Name = "layoutMessage";
            this.layoutMessage.RowCount = 1;
            this.layoutMessage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutMessage.Size = new System.Drawing.Size(687, 66);
            this.layoutMessage.TabIndex = 14;
            // 
            // Message
            // 
            this.Message.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Message.AutoSize = true;
            this.Message.Location = new System.Drawing.Point(71, 25);
            this.Message.Margin = new System.Windows.Forms.Padding(5);
            this.Message.MinimumSize = new System.Drawing.Size(175, 0);
            this.Message.Name = "Message";
            this.Message.Size = new System.Drawing.Size(175, 16);
            this.Message.TabIndex = 16;
            this.Message.Text = "Save changes to this file?";
            // 
            // img
            // 
            this.img.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.img.BackColor = System.Drawing.Color.Transparent;
            this.img.Location = new System.Drawing.Point(9, 9);
            this.img.Margin = new System.Windows.Forms.Padding(9);
            this.img.Name = "img";
            this.img.Size = new System.Drawing.Size(48, 48);
            this.img.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.img.TabIndex = 17;
            this.img.TabStop = false;
            this.img.Visible = false;
            // 
            // pnlLine
            // 
            this.pnlLine.AutoSize = true;
            this.pnlLine.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlLine.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.pnlLine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLine.Location = new System.Drawing.Point(0, 66);
            this.pnlLine.Margin = new System.Windows.Forms.Padding(0);
            this.pnlLine.Name = "pnlLine";
            this.pnlLine.Size = new System.Drawing.Size(687, 1);
            this.pnlLine.TabIndex = 20;
            // 
            // info
            // 
            this.info.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.info.BackColor = System.Drawing.Color.Transparent;
            this.info.Image = ((System.Drawing.Image) (resources.GetObject("info.Image")));
            this.info.Location = new System.Drawing.Point(245, 0);
            this.info.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
            this.info.Name = "info";
            this.info.Size = new System.Drawing.Size(48, 48);
            this.info.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.info.TabIndex = 18;
            this.info.TabStop = false;
            this.info.Visible = false;
            // 
            // question
            // 
            this.question.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.question.BackColor = System.Drawing.Color.Transparent;
            this.question.Image = ((System.Drawing.Image) (resources.GetObject("question.Image")));
            this.question.Location = new System.Drawing.Point(319, 0);
            this.question.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
            this.question.Name = "question";
            this.question.Size = new System.Drawing.Size(48, 48);
            this.question.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.question.TabIndex = 19;
            this.question.TabStop = false;
            this.question.Visible = false;
            // 
            // warning
            // 
            this.warning.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.warning.BackColor = System.Drawing.Color.Transparent;
            this.warning.Image = ((System.Drawing.Image) (resources.GetObject("warning.Image")));
            this.warning.Location = new System.Drawing.Point(394, 0);
            this.warning.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
            this.warning.Name = "warning";
            this.warning.Size = new System.Drawing.Size(48, 48);
            this.warning.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.warning.TabIndex = 20;
            this.warning.TabStop = false;
            this.warning.Visible = false;
            // 
            // error
            // 
            this.error.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.error.BackColor = System.Drawing.Color.Transparent;
            this.error.Image = ((System.Drawing.Image) (resources.GetObject("error.Image")));
            this.error.Location = new System.Drawing.Point(469, 0);
            this.error.Margin = new System.Windows.Forms.Padding(9, 0, 9, 0);
            this.error.Name = "error";
            this.error.Size = new System.Drawing.Size(48, 48);
            this.error.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.error.TabIndex = 21;
            this.error.TabStop = false;
            this.error.Visible = false;
            // 
            // DlgMessageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(687, 169);
            this.Controls.Add(this.error);
            this.Controls.Add(this.warning);
            this.Controls.Add(this.question);
            this.Controls.Add(this.info);
            this.Controls.Add(this.layoutMain);
            this.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DlgMessageForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DlgMessage_FormClosing);
            this.layoutMain.ResumeLayout(false);
            this.layoutMain.PerformLayout();
            this.layoutButtons.ResumeLayout(false);
            this.layoutButtons.PerformLayout();
            this.layoutMessage.ResumeLayout(false);
            this.layoutMessage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.img)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.info)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.question)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.warning)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.error)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel layoutMain;
        private System.Windows.Forms.TableLayoutPanel layoutButtons;
        public System.Windows.Forms.Button Btn2;
        public System.Windows.Forms.Button Btn3;
        public System.Windows.Forms.Button Btn0;
        public System.Windows.Forms.Button Btn1;
        private System.Windows.Forms.TableLayoutPanel layoutMessage;
        public System.Windows.Forms.Label Message;
        public System.Windows.Forms.PictureBox img;
        public System.Windows.Forms.PictureBox info;
        public System.Windows.Forms.PictureBox question;
        public System.Windows.Forms.PictureBox warning;
        public System.Windows.Forms.PictureBox error;
        private System.Windows.Forms.Panel pnlLine;

    }
}