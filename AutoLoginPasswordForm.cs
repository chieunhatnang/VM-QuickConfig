using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    public partial class AutoLoginPasswordForm : Form
    {

        public string autoLoginPassword { get; set; }

        public AutoLoginPasswordForm()
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
        }

        private void btnSubmitAutoLoginPassword_Click(object sender, EventArgs e)
        {
            submitPassword();
        }

        private void txtAutoLoginPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                submitPassword();
            }
        }

        private void submitPassword()
        {
            this.autoLoginPassword = txtAutoLoginPassword.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
