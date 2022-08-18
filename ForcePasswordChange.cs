using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    public partial class ForcePasswordChange : Form
    {
        public string oldPassword { get; set; }
        public string newPassword { get; set; }

        public ForcePasswordChange()
        {
            InitializeComponent();
        }

        private void btnForceChangePassword_Click(object sender, EventArgs e)
        {
            if (txtNewPassword.Text == txtNewPassword2.Text)
            {
                submitPassword();
            } else
            {
                MessageBox.Show("NEW Passwords not match!");
            }
        }

        private void txtNewPassword_TextChanged(object sender, EventArgs e)
        {
            if (isStrongPassword(txtNewPassword.Text))
            {
                lblPasswordStrength.Text = "Good";
                lblPasswordStrength.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblPasswordStrength.Text = "Weak";
                lblPasswordStrength.ForeColor = System.Drawing.Color.Red;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_NOCLOSE = 0x200;

                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_NOCLOSE;
                return cp;
            }
        }

        private void txtNewPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                submitPassword();
            }
        }

        private Boolean isStrongPassword(string password)
        {
            string[] badPasswordList = null;
            try
            {
                badPasswordList = File.ReadAllText("C:\\Users\\Public\\LEV\\bad_pw.txt").Split(',');
            } catch
            {

            }
            
            if (password.Length >= 8 && (password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit))
                && (badPasswordList == null || !badPasswordList.Contains(password)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void submitPassword()
        {
            if (!isStrongPassword(txtNewPassword.Text))
            {
                MessageBox.Show("Password must be:\r\n- More than 8 characters\r\n- Contains UPPER CASE leters (A-Z)\r\n- Contains lower case letter (a-z)\r\n- Contains number (0-9)");
                return;
            }
            this.oldPassword = txtOldPassword.Text;
            this.newPassword = txtNewPassword.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
