namespace OpenMFA.GUI
{
    partial class MainForm
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
            this.groupBoxCardInfo = new System.Windows.Forms.GroupBox();
            this.btnDetect = new System.Windows.Forms.Button();
            this.btnInfo = new System.Windows.Forms.Button();
            this.btnList = new System.Windows.Forms.Button();
            this.groupBoxCertificates = new System.Windows.Forms.GroupBox();
            this.labelSlot = new System.Windows.Forms.Label();
            this.comboBoxSlot = new System.Windows.Forms.ComboBox();
            this.btnRead = new System.Windows.Forms.Button();
            this.btnWrite = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.groupBoxKeyGeneration = new System.Windows.Forms.GroupBox();
            this.labelKeySlot = new System.Windows.Forms.Label();
            this.comboBoxKeySlot = new System.Windows.Forms.ComboBox();
            this.labelAlgorithm = new System.Windows.Forms.Label();
            this.comboBoxAlgorithm = new System.Windows.Forms.ComboBox();
            this.btnGenerateKey = new System.Windows.Forms.Button();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.labelOutput = new System.Windows.Forms.Label();
            this.groupBoxCardInfo.SuspendLayout();
            this.groupBoxCertificates.SuspendLayout();
            this.groupBoxKeyGeneration.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBoxCardInfo
            //
            this.groupBoxCardInfo.Controls.Add(this.btnDetect);
            this.groupBoxCardInfo.Controls.Add(this.btnInfo);
            this.groupBoxCardInfo.Controls.Add(this.btnList);
            this.groupBoxCardInfo.Location = new System.Drawing.Point(12, 12);
            this.groupBoxCardInfo.Name = "groupBoxCardInfo";
            this.groupBoxCardInfo.Size = new System.Drawing.Size(760, 80);
            this.groupBoxCardInfo.TabIndex = 0;
            this.groupBoxCardInfo.TabStop = false;
            this.groupBoxCardInfo.Text = "Card Information";
            //
            // btnDetect
            //
            this.btnDetect.Location = new System.Drawing.Point(20, 30);
            this.btnDetect.Name = "btnDetect";
            this.btnDetect.Size = new System.Drawing.Size(120, 30);
            this.btnDetect.TabIndex = 0;
            this.btnDetect.Text = "Detect Card";
            this.btnDetect.UseVisualStyleBackColor = true;
            this.btnDetect.Click += new System.EventHandler(this.BtnDetect_Click);
            //
            // btnInfo
            //
            this.btnInfo.Location = new System.Drawing.Point(160, 30);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Size = new System.Drawing.Size(120, 30);
            this.btnInfo.TabIndex = 1;
            this.btnInfo.Text = "Card Info";
            this.btnInfo.UseVisualStyleBackColor = true;
            this.btnInfo.Click += new System.EventHandler(this.BtnInfo_Click);
            //
            // btnList
            //
            this.btnList.Location = new System.Drawing.Point(300, 30);
            this.btnList.Name = "btnList";
            this.btnList.Size = new System.Drawing.Size(120, 30);
            this.btnList.TabIndex = 2;
            this.btnList.Text = "List Certificates";
            this.btnList.UseVisualStyleBackColor = true;
            this.btnList.Click += new System.EventHandler(this.BtnList_Click);
            //
            // groupBoxCertificates
            //
            this.groupBoxCertificates.Controls.Add(this.labelSlot);
            this.groupBoxCertificates.Controls.Add(this.comboBoxSlot);
            this.groupBoxCertificates.Controls.Add(this.btnRead);
            this.groupBoxCertificates.Controls.Add(this.btnWrite);
            this.groupBoxCertificates.Controls.Add(this.btnDelete);
            this.groupBoxCertificates.Location = new System.Drawing.Point(12, 100);
            this.groupBoxCertificates.Name = "groupBoxCertificates";
            this.groupBoxCertificates.Size = new System.Drawing.Size(760, 100);
            this.groupBoxCertificates.TabIndex = 1;
            this.groupBoxCertificates.TabStop = false;
            this.groupBoxCertificates.Text = "Certificate Management";
            //
            // labelSlot
            //
            this.labelSlot.AutoSize = true;
            this.labelSlot.Location = new System.Drawing.Point(20, 30);
            this.labelSlot.Name = "labelSlot";
            this.labelSlot.Size = new System.Drawing.Size(30, 15);
            this.labelSlot.TabIndex = 0;
            this.labelSlot.Text = "Slot:";
            //
            // comboBoxSlot
            //
            this.comboBoxSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSlot.FormattingEnabled = true;
            this.comboBoxSlot.Location = new System.Drawing.Point(60, 27);
            this.comboBoxSlot.Name = "comboBoxSlot";
            this.comboBoxSlot.Size = new System.Drawing.Size(200, 23);
            this.comboBoxSlot.TabIndex = 1;
            //
            // btnRead
            //
            this.btnRead.Location = new System.Drawing.Point(20, 60);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(120, 30);
            this.btnRead.TabIndex = 2;
            this.btnRead.Text = "Read Certificate";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.BtnRead_Click);
            //
            // btnWrite
            //
            this.btnWrite.Location = new System.Drawing.Point(160, 60);
            this.btnWrite.Name = "btnWrite";
            this.btnWrite.Size = new System.Drawing.Size(120, 30);
            this.btnWrite.TabIndex = 3;
            this.btnWrite.Text = "Write Certificate";
            this.btnWrite.UseVisualStyleBackColor = true;
            this.btnWrite.Click += new System.EventHandler(this.BtnWrite_Click);
            //
            // btnDelete
            //
            this.btnDelete.Location = new System.Drawing.Point(300, 60);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(120, 30);
            this.btnDelete.TabIndex = 4;
            this.btnDelete.Text = "Delete Certificate";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            //
            // groupBoxKeyGeneration
            //
            this.groupBoxKeyGeneration.Controls.Add(this.labelKeySlot);
            this.groupBoxKeyGeneration.Controls.Add(this.comboBoxKeySlot);
            this.groupBoxKeyGeneration.Controls.Add(this.labelAlgorithm);
            this.groupBoxKeyGeneration.Controls.Add(this.comboBoxAlgorithm);
            this.groupBoxKeyGeneration.Controls.Add(this.btnGenerateKey);
            this.groupBoxKeyGeneration.Location = new System.Drawing.Point(12, 210);
            this.groupBoxKeyGeneration.Name = "groupBoxKeyGeneration";
            this.groupBoxKeyGeneration.Size = new System.Drawing.Size(760, 100);
            this.groupBoxKeyGeneration.TabIndex = 2;
            this.groupBoxKeyGeneration.TabStop = false;
            this.groupBoxKeyGeneration.Text = "Key Generation";
            //
            // labelKeySlot
            //
            this.labelKeySlot.AutoSize = true;
            this.labelKeySlot.Location = new System.Drawing.Point(20, 30);
            this.labelKeySlot.Name = "labelKeySlot";
            this.labelKeySlot.Size = new System.Drawing.Size(30, 15);
            this.labelKeySlot.TabIndex = 0;
            this.labelKeySlot.Text = "Slot:";
            //
            // comboBoxKeySlot
            //
            this.comboBoxKeySlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxKeySlot.FormattingEnabled = true;
            this.comboBoxKeySlot.Location = new System.Drawing.Point(60, 27);
            this.comboBoxKeySlot.Name = "comboBoxKeySlot";
            this.comboBoxKeySlot.Size = new System.Drawing.Size(200, 23);
            this.comboBoxKeySlot.TabIndex = 1;
            //
            // labelAlgorithm
            //
            this.labelAlgorithm.AutoSize = true;
            this.labelAlgorithm.Location = new System.Drawing.Point(280, 30);
            this.labelAlgorithm.Name = "labelAlgorithm";
            this.labelAlgorithm.Size = new System.Drawing.Size(65, 15);
            this.labelAlgorithm.TabIndex = 2;
            this.labelAlgorithm.Text = "Algorithm:";
            //
            // comboBoxAlgorithm
            //
            this.comboBoxAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAlgorithm.FormattingEnabled = true;
            this.comboBoxAlgorithm.Location = new System.Drawing.Point(350, 27);
            this.comboBoxAlgorithm.Name = "comboBoxAlgorithm";
            this.comboBoxAlgorithm.Size = new System.Drawing.Size(200, 23);
            this.comboBoxAlgorithm.TabIndex = 3;
            //
            // btnGenerateKey
            //
            this.btnGenerateKey.Location = new System.Drawing.Point(20, 60);
            this.btnGenerateKey.Name = "btnGenerateKey";
            this.btnGenerateKey.Size = new System.Drawing.Size(120, 30);
            this.btnGenerateKey.TabIndex = 4;
            this.btnGenerateKey.Text = "Generate Key Pair";
            this.btnGenerateKey.UseVisualStyleBackColor = true;
            this.btnGenerateKey.Click += new System.EventHandler(this.BtnGenerateKey_Click);
            //
            // labelOutput
            //
            this.labelOutput.AutoSize = true;
            this.labelOutput.Location = new System.Drawing.Point(12, 320);
            this.labelOutput.Name = "labelOutput";
            this.labelOutput.Size = new System.Drawing.Size(48, 15);
            this.labelOutput.TabIndex = 3;
            this.labelOutput.Text = "Output:";
            //
            // textBoxOutput
            //
            this.textBoxOutput.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBoxOutput.Location = new System.Drawing.Point(12, 340);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ReadOnly = true;
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxOutput.Size = new System.Drawing.Size(760, 200);
            this.textBoxOutput.TabIndex = 4;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.textBoxOutput);
            this.Controls.Add(this.labelOutput);
            this.Controls.Add(this.groupBoxKeyGeneration);
            this.Controls.Add(this.groupBoxCertificates);
            this.Controls.Add(this.groupBoxCardInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OpenMFA - PIV Smart Card Manager";
            this.groupBoxCardInfo.ResumeLayout(false);
            this.groupBoxCertificates.ResumeLayout(false);
            this.groupBoxCertificates.PerformLayout();
            this.groupBoxKeyGeneration.ResumeLayout(false);
            this.groupBoxKeyGeneration.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxCardInfo;
        private System.Windows.Forms.Button btnDetect;
        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.Button btnList;
        private System.Windows.Forms.GroupBox groupBoxCertificates;
        private System.Windows.Forms.Label labelSlot;
        private System.Windows.Forms.ComboBox comboBoxSlot;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.Button btnWrite;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.GroupBox groupBoxKeyGeneration;
        private System.Windows.Forms.Label labelKeySlot;
        private System.Windows.Forms.ComboBox comboBoxKeySlot;
        private System.Windows.Forms.Label labelAlgorithm;
        private System.Windows.Forms.ComboBox comboBoxAlgorithm;
        private System.Windows.Forms.Button btnGenerateKey;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.Label labelOutput;
    }
}
