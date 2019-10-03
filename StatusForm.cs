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
    public partial class StatusForm : Form
    {
        List<LevCheckbox> levCheckboxList;

        public StatusForm()
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
        }

        public StatusForm(List<LevCheckbox> levCheckboxList)
        {
            InitializeComponent();
            this.levCheckboxList = levCheckboxList;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void updateProgress()
        {
            if (this.rtbProgress.InvokeRequired)
            {
                DelegateUpdateProgress d = new DelegateUpdateProgress(updateProgress);
                this.Invoke(d);
            }
            else
            {
                this.rtbProgress.Text = "";
                foreach (LevCheckbox levCheckbox in levCheckboxList)
                {
                    if (levCheckbox.checkBox.Checked)
                    {
                        this.rtbProgress.Text += levCheckbox.status + Environment.NewLine;
                    }
                }
                this.rtbProgress.Update();
            }
        }

        private delegate void DelegateUpdateProgress();
    }
}
