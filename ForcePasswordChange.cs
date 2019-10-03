using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    public partial class ForcePasswordChange : Form
    {
        public string newPassword { get; set; }

        public ForcePasswordChange()
        {
            InitializeComponent();
        }

        private void btnForceChangePassword_Click(object sender, EventArgs e)
        {
            submitPassword();
        }

        private void txtNewPassword_TextChanged(object sender, EventArgs e)
        {
            if (txtNewPassword.Text.Length >= 8 && (txtNewPassword.Text.Any(char.IsUpper) && txtNewPassword.Text.Any(char.IsLower) && txtNewPassword.Text.Any(char.IsDigit)))
            {
                btnForceChangePassword.Enabled = true;
            }
            else
            {
                btnForceChangePassword.Enabled = false;
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
            if (e.KeyCode == Keys.Enter && txtNewPassword.Text.Length >= 8 && (txtNewPassword.Text.Any(char.IsUpper) && txtNewPassword.Text.Any(char.IsLower) && txtNewPassword.Text.Any(char.IsDigit)))
            {
                submitPassword();
            }
        }

        private void submitPassword()
        {
            this.newPassword = txtNewPassword.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
