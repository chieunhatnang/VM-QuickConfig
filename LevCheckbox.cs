using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    public class LevCheckbox
    {
        public CheckBox checkBox { get; set; }
        public string status { get; set; }
        public string command { get; set; }
        public string softwareURL { get; set; }
        public string setupFileName { get; set; }
        public string remark { get; set; }

        public LevCheckbox(CheckBox chk, string command)
        {
            this.checkBox = chk;
            this.command = command;
            this.status = checkBox.Text + " >>> Waiting.....";
        }

        public LevCheckbox(CheckBox chk, string url, string fileName, string command, string remark = null)
        {
            this.checkBox = chk;
            this.command = command;
            this.softwareURL = url;
            this.setupFileName = fileName;
            this.status = checkBox.Text + " >>> Waiting.....";
            this.remark = remark;
        }

        public void updateResultStatus(int exitCode)
        {
            if (exitCode == 0)
            {
                this.status = checkBox.Text + " >>> Success!";
            }
            else
            {
                this.status = checkBox.Text + " >>> Error!";
            }
            if (remark != null)
            {
                this.status += this.remark;
            }
        }

        public void updateInstallingStatus()
        {
            this.status = checkBox.Text + " >>> Installing...";
        }

        public void updateDownloadingStatus()
        {
            this.status = checkBox.Text + " >>> Downloading...";
        }
    }
}
