namespace LowEndViet.com_VPS_Tool
{
    partial class AutoLoginPasswordForm
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
            this.txtAutoLoginPassword = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSubmitAutoLoginPassword = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtAutoLoginPassword
            // 
            this.txtAutoLoginPassword.Location = new System.Drawing.Point(96, 55);
            this.txtAutoLoginPassword.Name = "txtAutoLoginPassword";
            this.txtAutoLoginPassword.Size = new System.Drawing.Size(176, 20);
            this.txtAutoLoginPassword.TabIndex = 0;
            this.txtAutoLoginPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtAutoLoginPassword_KeyDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 58);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Your password";
            // 
            // btnSubmitAutoLoginPassword
            // 
            this.btnSubmitAutoLoginPassword.Location = new System.Drawing.Point(96, 90);
            this.btnSubmitAutoLoginPassword.Name = "btnSubmitAutoLoginPassword";
            this.btnSubmitAutoLoginPassword.Size = new System.Drawing.Size(89, 23);
            this.btnSubmitAutoLoginPassword.TabIndex = 2;
            this.btnSubmitAutoLoginPassword.Text = "OK";
            this.btnSubmitAutoLoginPassword.UseVisualStyleBackColor = true;
            this.btnSubmitAutoLoginPassword.Click += new System.EventHandler(this.btnSubmitAutoLoginPassword_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(248, 26);
            this.label2.TabIndex = 3;
            this.label2.Text = "In order for this program to run at startup, you have \r\nto enter the CURRENT pass" +
    "word.";
            // 
            // AutoLoginPasswordForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 123);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnSubmitAutoLoginPassword);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtAutoLoginPassword);
            this.Name = "AutoLoginPasswordForm";
            this.Text = "Password";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtAutoLoginPassword;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSubmitAutoLoginPassword;
        private System.Windows.Forms.Label label2;
    }
}