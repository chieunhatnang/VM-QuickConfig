using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    public partial class form_LowEndVietFastVPSConfig : Form
    {
        #region Final variables
        static readonly string APPNAME = "VM QuickConfig";
        public readonly string VERSION = "1.6";
        static readonly string GITNAME = "VM QuickConfig";
        static readonly string GITHOME = "https://github.com/chieunhatnang/VM-QuickConfig";

        static readonly string REG_STARTUP = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\";
        static readonly string REG_LEV = "Software\\LEV\\VMQuickConfig";
        static readonly string REG_RDP_PORT = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp";
        static readonly string LEV_DIR = "C:\\Users\\Public\\LEV\\";
        static readonly string DISKPART_CONFIG_PATH = LEV_DIR + "diskpartconfig.txt";
        static readonly string NETWORK_CONFIG_PATH = LEV_DIR + "networkconfig.txt";

        static readonly int ALLOW_RANDOM_IPV6_QUARTET = 3;

        #endregion

        #region Global variables
        public static List<DNSConfig> DNSServerList;
        public static List<DNSConfig> DNSServerListIPv6;

        public List<LevCheckbox> levCheckbox4WindowsList;
        public List<LevCheckbox> levCheckbox4Software;

        RegistryKey LEVStartupKey;
        RegistryKey LEVRegKey;

        string currentUsername;
        #endregion

        public form_LowEndVietFastVPSConfig(string[] args)
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.Text = this.Text + " Version " + VERSION;

            this.lnkGit.Text = GITNAME;
            //Set the tooltip
            ttAutologin.SetToolTip(label8, "If you check this box, your VPS will be automatically login when it is started.\r\n" +
                "It allows you to reset your password over Web console in case you forget the password.");

            InitCheckbox();
            InitRegistry();
            InitLEVDir();

            // Initialize combobox
            this.cmbDNS.Items.AddRange(DNSServerList.ToArray());
            cmbDNS.DropDownWidth = 200;
            this.cbbDNSV6.Items.AddRange(DNSServerListIPv6.ToArray());
            cbbDNSV6.DropDownWidth = 250;

            if (File.Exists(NETWORK_CONFIG_PATH))
            {
                LoadNetworkConfigFile(NETWORK_CONFIG_PATH);
            }
            else
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.CDRom))
                    if (drive.IsReady)
                    {
                        string configOnCD = drive.RootDirectory.ToString() + "config.txt";
                        if (File.Exists(configOnCD))
                        {
                            LoadNetworkConfigFile(configOnCD);
                        }
                    }
            }

            // Get & set current username which runs the app
            string fullUsername = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            if (fullUsername.Contains("\\"))
            {
                currentUsername = fullUsername.Split('\\')[fullUsername.Split('\\').Length - 1];
            }
            else
            {
                currentUsername = fullUsername;
            }
            txtAdminAcc.Text = currentUsername;

            // Get & set current RDP port
            lblCurrentRDPPort.Text = Registry.LocalMachine.OpenSubKey(REG_RDP_PORT).GetValue("PortNumber").ToString();

            // Time zone
            this.cbbTimeZone.Items.AddRange(TimeZoneInfo.GetSystemTimeZones().ToArray());
            this.cbbTimeZone.DisplayMember = "DisplayName";
            this.cbbTimeZone.ValueMember = "Id";
            this.cbbTimeZone.SelectedItem = TimeZoneInfo.GetSystemTimeZones().ToArray().FirstOrDefault(x => x.StandardName == TimeZone.CurrentTimeZone.StandardName);

            cbEnableIPv6.Checked = IsIPv6Enable();
        }

        #region Event Processing
        private void Form_LowEndVietFastVPSConfig_Load(object sender, EventArgs e)
        {
            // Check and force change password
            if (LEVRegKey.GetValue("ForceChangePassword").ToString() == "1")
            {
                ExecuteCommand("taskkill /IM explorer.exe /F", true);
                ForcePasswordChange frm = new ForcePasswordChange();
                bool changePasswordResult = false;
                while (!changePasswordResult)
                {
                    var formResult = frm.ShowDialog();
                    if (formResult == DialogResult.OK)
                    {
                        changePasswordResult = changePassword("Administrator", frm.oldPassword, frm.newPassword);
                        if (changePasswordResult)
                        {
                            ExecuteCommand("explorer.exe");
                            SetupAutoLogin(frm.newPassword);
                            LEVRegKey.SetValue("ForceChangePassword", 0);
                            chkForceChangePass.Checked = false;
                        }
                    }
                }
            }
        }


        private void RdDHCP_CheckedChanged(object sender, EventArgs e)
        {
            txtIP.Enabled = rdStatic.Checked;
            txtNetmask.Enabled = rdStatic.Checked;
            txtGateway.Enabled = rdStatic.Checked;
        }
        private void RbDHCPV6_CheckedChanged(object sender, EventArgs e)
        {
            txtIPV6.Enabled = rbStaticV6.Checked;
            txtNetmaskV6.Enabled = rbStaticV6.Checked;
            txtGatewayV6.Enabled = rbStaticV6.Checked;
        }

        private void BtnConfigIPv4Network_Click(object sender, EventArgs e)
        {
            try
            {
                if (rdStatic.Checked)
                {
                    SetIPv4Static(txtIP.Text, txtNetmask.Text, txtGateway.Text, txtCustomDNS.Text);
                    WriteConfigFile();
                }
                if (rdDHCP.Checked)
                {
                    SetIPv4DHCP(txtCustomDNS.Text);
                }
                MessageBox.Show("Successfully set your network IPv4 configuration!", "Success!",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when setting IPv4!\r\n" + ex.Message, "Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void btnRandomIPv6_Click(object sender, EventArgs e)
        {
            string currentIPv6FullFormat = ConvertIPv6ToFullFormat(txtIPV6.Text);
            if (currentIPv6FullFormat == null)
            {
                MessageBox.Show("Invalid IPv6 subnet!", "Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                txtIPV6.Text = string.Join(":", currentIPv6FullFormat
                    .Split(new string[] { ":" }, StringSplitOptions.None)
                    .Skip(0).Take(8 - ALLOW_RANDOM_IPV6_QUARTET).ToArray()) + ":" + GenerateRandomIPv6Quartet(ALLOW_RANDOM_IPV6_QUARTET);
            }        
        }

        private void BtnConfigIPv6Network_Click(object sender, EventArgs e)
        {
            var currentInterface = GetActiveEthernetOrWifiNetworkInterface();
            if (cbEnableIPv6.Checked)
            {
                if (rbStaticV6.Checked)
                {
                    string currentIPv6FullFormat = ConvertIPv6ToFullFormat(txtIPV6.Text);
                    if (currentIPv6FullFormat == null)
                    {
                        MessageBox.Show("Invalid IPv6 subnet!", "Error!",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                    else
                    {
                        if (currentInterface == null)
                        {
                            MessageBox.Show("No active interface!", "Error!",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                        try
                        {
                            SetIPv6DHCP(currentInterface, false);
                            ClearIPv6(currentInterface);
                            SetIPv6Static(currentInterface, currentIPv6FullFormat, txtNetmaskV6.Text, txtGatewayV6.Text);
                            SetIPv6DNS(txtCustomDNSV6.Text);
                            WriteConfigFile();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "Error!",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                    }

                }
                if (rbDHCPV6.Checked)
                {
                    try
                    {
                        ClearIPv6(currentInterface);
                        SetIPv6DHCP(currentInterface, true);
                        SetIPv6DNS(txtCustomDNSV6.Text);
                    }
                    catch (Exception) {
                        MessageBox.Show("Error occur when setting DHCP for IPv6!", "Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }
            }
            else
            {
                DisableIPv6(currentInterface);
            }
            MessageBox.Show("Successfully set your network IPv6 configuration!", "Success!",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CmbDNS_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtCustomDNS.Text = ((DNSConfig)cmbDNS.SelectedItem).DNS1;
            if (((DNSConfig)cmbDNS.SelectedItem).DNS1 == "")
            {
                txtCustomDNS.Enabled = true;
            }
            else
            {
                txtCustomDNS.Enabled = false;
            }
        }

        private void CbbDNSV6_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtCustomDNSV6.Text = ((DNSConfig)cbbDNSV6.SelectedItem).DNS1;
            if (((DNSConfig)cbbDNSV6.SelectedItem).DNS1 == "")
            {
                txtCustomDNSV6.Enabled = true;
            }
            else
            {
                txtCustomDNSV6.Enabled = false;
            }
        }

        private void BtnChangePassword_Click(object sender, EventArgs e)
        {
            try
            {
                string adminAcc = txtAdminAcc.Text;
                DialogResult dialogResult = MessageBox.Show("You are changing the password of username \"" + adminAcc + "\"" +
                    "\r\nIf \"" + adminAcc + "\" is not your Administrator account, please enter your Administrator account on the text box \"Change Administrator acc.\" above.\r\n" +
                    "The next time, pleaes login with the new credentials: \r\n\r\n" +
                    adminAcc + "\r\n" +
                    txtNewPassword.Text,
                    "Change password for " + adminAcc + " ?", MessageBoxButtons.OKCancel);
                if (dialogResult == DialogResult.OK)
                {
                    changePassword(adminAcc, txtOldPassword.Text, txtNewPassword.Text);
                }

                if (chkAutoLogin.Checked)
                {
                    SetupAutoLogin(txtNewPassword.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when changing password!\r\n" + ex.Message, "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

        }

        private void BtnChangeAdminAcc_Click(object sender, EventArgs e)
        {
            try
            {
                string newAdminAcc = txtAdminAcc.Text;
                Regex rgx = new Regex("[^A-Za-z0-9]");
                if (rgx.IsMatch(newAdminAcc))
                {
                    MessageBox.Show("Username cannot contain special character!", "Error!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                }
                else
                {
                    DialogResult dialogResult = MessageBox.Show("You are renaming your account to " + newAdminAcc +
                        ". You may need to restart to apply the change. Please re-login using the new username:" + "\r\n" +
                    "IMPORTANT NOTE: You may lost encrypted data like Cookies, files in Bitlocker drives... after changing the account name." + "\r\n\r\n" + newAdminAcc,
                    "Rename account to " + newAdminAcc + " ?", MessageBoxButtons.OKCancel);
                    if (dialogResult == DialogResult.OK)
                    {
                        if (dialogResult == DialogResult.OK)
                        {
                            ExecuteCommand("wmic useraccount where name='" + currentUsername + "' call rename name='" + txtAdminAcc.Text + "'");
                            DialogResult dialogResult2 = MessageBox.Show("Successfully renamed your account to " + newAdminAcc + ". Do you want to RESTART now?",
                                "Success!", MessageBoxButtons.YesNo);
                            if (dialogResult2 == DialogResult.Yes)
                            {
                                ExecuteCommand("shutdown /r /t 5");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when changing Admin account!\r\n" + ex.Message, "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

        }

        private void BtnChangeRDPPort_Click(object sender, EventArgs e)
        {
            try
            {
                string newRDPPort = txtRDPPort.Text;
                Regex rgx = new Regex("[^0-9]");
                if (rgx.IsMatch(newRDPPort) || int.Parse(newRDPPort) < 1000 || int.Parse(newRDPPort) > 65530)
                {
                    MessageBox.Show("RDP port should be 1000 < port < 65000!", "Error!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                }
                else
                {
                    DialogResult dialogResult = MessageBox.Show("You are changing RDP port to " + newRDPPort + ". After press OK, you will be DISCONNETED!!!\r\nPlease connect to the following address instead:\r\n\r\n" + txtIP.Text + ":" + newRDPPort,
                    "Change remote port to " + newRDPPort + " ?", MessageBoxButtons.OKCancel);
                    if (dialogResult == DialogResult.OK)
                    {
                        string portHexa = "0x" + int.Parse(newRDPPort).ToString("X");
                        ExecuteCommand("reg add \"HKEY_LOCAL_MACHINE\\" + REG_RDP_PORT + "\" /v PortNumber /t REG_DWORD /d " + portHexa + " /f");
                        ExecuteCommand("netsh advfirewall firewall add rule name = \"Secure RDP on port " + newRDPPort + "\" dir =in action = allow protocol = TCP localport = " + newRDPPort);
                        ExecuteCommand("net stop \"TermService\" /y && net start \"TermService\"");
                        MessageBox.Show("Successfully change RDP port!", "Success!",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        lblCurrentRDPPort.Text = newRDPPort;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when changing RDP remote port!\r\n" + ex.Message, "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
        }

        private void BtnOpenFWPort_Click(object sender, EventArgs e)
        {
            try
            {
                string port2Open = txtPort2Open.Text;
                Regex rgx = new Regex("[^0-9]");
                if (rgx.IsMatch(port2Open) || int.Parse(port2Open) < -1 || int.Parse(port2Open) > 65535)
                {
                    MessageBox.Show("Port to open must be 1 < port < 65535!", "Error!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                }
                else
                {
                    DialogResult dialogResult = MessageBox.Show("Are you sure to open the following port for incoming connection in firewall:\r\n\r\n" + port2Open + "\r\n\r\n" +
                       "Always remember that opening incoming port may lead to security problem.",
                    "Open port " + port2Open + " in firewall ?", MessageBoxButtons.OKCancel);
                    if (dialogResult == DialogResult.OK)
                    {
                        string portHexa = "0x" + int.Parse(port2Open).ToString("X");
                        ExecuteCommand("netsh advfirewall firewall add rule name = \"Open custom port " + port2Open + "\" dir =in action = allow protocol = TCP localport = " + port2Open);
                        MessageBox.Show("Successfully open port " + port2Open + "!", "Success!",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when open firewall for incoming port!\r\n" + ex.Message, "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
        }

        private void BtnExtendDisk_Click(object sender, EventArgs e)
        {
            try
            {
                ExtendDisk();
                MessageBox.Show("Successfully extend your disk to maximum capacity!", "Success!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when enxtending disk!\r\n" + ex.Message, "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

        }

        private void btnDiskManagement_Click(object sender, EventArgs e)
        {
            ExecuteCommand("diskmgmt.msc", true);
        }


        private void BtnCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (LevCheckbox levCheckbox in levCheckbox4WindowsList)
            {
                levCheckbox.checkBox.Checked = chkCheckAll.Checked;
            }
        }

        private void BtnConfigWindows_Click(object sender, EventArgs e)
        {
            try
            {
                StatusForm statusForm = new StatusForm(levCheckbox4WindowsList);
                statusForm.Show();
                var timeZoneInfo = this.cbbTimeZone.SelectedItem as TimeZoneInfo;
                Thread t = new Thread(() =>
                {
                    foreach (LevCheckbox levCheckbox in levCheckbox4WindowsList)
                    {
                        if (levCheckbox.checkBox.Checked)
                        {
                            levCheckbox.updateResultStatus(ExecuteCommand(levCheckbox.command, true));
                            statusForm.updateProgress();
                        }
                    }


                    // Change timezone
                    ExecuteCommand("tzutil.exe /s \"" + timeZoneInfo.Id + "\"", true);
                    statusForm.updateProgress("Changed system timezone to " + timeZoneInfo.Id + ".");
                    ExecuteCommand("taskkill /IM explorer.exe /F & explorer.exe", true);
                });
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur configuring Windows!\r\n" + ex.Message, "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
        }


        private void BtnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Add firefox
                if (txtFirefoxVer.Text == "Latest")
                {
                    levCheckbox4Software.Add(new LevCheckbox(chkFirefox, "https://download.mozilla.org/?product=firefox-latest&os=win&lang=en-US", "FirefoxLatest.exe", "FirefoxLatest.exe /S"));
                }
                else
                {
                    levCheckbox4Software.Add(new LevCheckbox(chkFirefox, "https://ftp.mozilla.org/pub/firefox/releases/" + txtFirefoxVer.Text + ".0/win32/en-US/Firefox%20Setup%20" + txtFirefoxVer.Text + ".0.exe", "FirefoxSetup.exe", "FirefoxSetup.exe /S"));
                }

                StatusForm statusForm = new StatusForm(levCheckbox4Software);
                statusForm.Show();
                WebClient wc = new WebClient();
                Task t = new Task(() =>
                {
                    foreach (LevCheckbox levCheckbox in levCheckbox4Software)
                    {
                        if (levCheckbox.checkBox.Checked)
                        {

                            ServicePointManager.Expect100Continue = true;
                            ServicePointManager.DefaultConnectionLimit = 9999;

                            // Limitation: .NET 3.5 doesn't support TLS 1.2
                            // ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls; // For .NET 3.5 and .NET 4
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12; //For .NET 4.5

                            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                            levCheckbox.updateDownloadingStatus();
                            statusForm.updateProgress();
                            try
                            {
                                wc.DownloadFile(levCheckbox.softwareURL, Path.GetTempPath() + levCheckbox.setupFileName);
                            }
                            catch (Exception ex)
                            {
                                levCheckbox.remark = ex.Message;
                            }
                            finally
                            {
                                levCheckbox.updateInstallingStatus();
                            }
                            statusForm.updateProgress();
                            levCheckbox.updateResultStatus(ExecuteCommand(Path.GetTempPath() + levCheckbox.command, true));
                            statusForm.updateProgress();
                        }
                    }
                    wc.Dispose();
                });
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occur when installing software!\r\n" + ex.Message, "Error!",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
        }

        private void ChkStartUp_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStartUp.Checked == true)
            {
                LEVStartupKey.SetValue(APPNAME, Application.ExecutablePath);
            }
            else
            {
                LEVStartupKey.DeleteValue(APPNAME, false);
            }
        }

        private void ChkUpdate_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUpdate.Checked == true)
            {
                LEVRegKey.SetValue("AutoUpdate", "1");
            }
            else
            {
                LEVRegKey.SetValue("AutoUpdate", "0");
            }
        }


        private void ChkForceChangePass_CheckedChanged(object sender, EventArgs e)
        {
            if (chkForceChangePass.Checked == true)
            {
                LEVRegKey.SetValue("ForceChangePassword", "1");
            }
            else
            {
                LEVRegKey.SetValue("ForceChangePassword", "0");
            }
        }

        private void lnkGit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(GITHOME);
        }


        private void Form_LowEndVietFastVPSConfig_FormClosed(object sender, FormClosedEventArgs e)
        {
            LEVStartupKey.Close();
            LEVRegKey.Close();
        }

        private void CbEnableIPv6_CheckedChanged(object sender, EventArgs e)
        {
            txtIPV6.Enabled = rbStaticV6.Checked && cbEnableIPv6.Checked;
            txtNetmaskV6.Enabled = rbStaticV6.Checked && cbEnableIPv6.Checked;
            txtGatewayV6.Enabled = rbStaticV6.Checked && cbEnableIPv6.Checked;
            cbbDNSV6.Enabled = cbEnableIPv6.Checked;
            txtCustomDNSV6.Enabled = cbEnableIPv6.Checked;
            rbDHCPV6.Enabled = cbEnableIPv6.Checked;
            rbStaticV6.Enabled = cbEnableIPv6.Checked;
        }

        #endregion

        #region Private functions
        private void InitCheckbox()
        {
            DNSServerList = new List<DNSConfig>(new DNSConfig[] {
            new DNSConfig("Google DNS", "8.8.8.8"),
            new DNSConfig("Cloudflare DNS", "1.1.1.1"),
            new DNSConfig("Cisco OpenDNS", "208.67.222.222"),
            new DNSConfig("VNNIC", "203.119.36.106"),
            new DNSConfig("CMC Telecom", "45.122.233.76"),
            new DNSConfig("VDC", "123.25.116.228"),
            new DNSConfig("VNPT 1", "14.160.3.78"),
            new DNSConfig("VNPT 2", "113.191.251.66"),
            new DNSConfig("LEV DNS", "103.185.185.185"),
            new DNSConfig("LEV DNS", "103.185.185.103"),
            new DNSConfig("Custom DNS", "")
            });

            DNSServerListIPv6 = new List<DNSConfig>(new DNSConfig[]
            {
                new DNSConfig("Google DNS", "2001:4860:4860::8888"),
                new DNSConfig("CloudFlare", "2606:4700:4700::64"),
                new DNSConfig("OpenDNS", "2620:119:35::35"),
                new DNSConfig("Custom DNS", "")
            });

            // Initialize LevCheckbox list for Windows config
            levCheckbox4WindowsList = new List<LevCheckbox>(new LevCheckbox[]
            {
                new LevCheckbox(chkDisableUAC, "reg ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v EnableLUA /t REG_DWORD /d 0 /f"),
                new LevCheckbox(chkDisableHiberfil, "powercfg.exe /hibernate off"),
                new LevCheckbox(chkTurnoffESC, "REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Active Setup\\Installed Components\\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}\" /v IsInstalled /t REG_DWORD /d 00000000 /f"
                                               + "&& REG ADD \"HKLM\\SOFTWARE\\Microsoft\\Active Setup\\Installed Components\\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}\" /v IsInstalled /t REG_DWORD /d 00000000 /f"),
                new LevCheckbox(chkDisableRecovery, "bcdedit /set {default} bootstatuspolicy ignoreallfailures"
                                                + " && bcdedit /set {default} recoveryenabled No"),
                new LevCheckbox(chkDisableUpdate, "reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\" /v AUOptions /t REG_DWORD /d 1 /f"),
                new LevCheckbox(chkDisableDriverSig, "bcdedit -set loadoptions DISABLE_INTEGRITY_CHECKS"),
                new LevCheckbox(chkShowTrayIcon, "reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\" /v EnableAutoTray /t REG_DWORD /d 0 /f"),
                new LevCheckbox(chkPerformanceRDP, "reg add \"HKEY_USERS\\.DEFAULT\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f"),
                new LevCheckbox(chkSmallTaskbar, "reg add \"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarSmallIcons /t REG_DWORD /d 1 /f"),
                new LevCheckbox(chkDeleteTempFolder, "DEL /F /S /Q %TEMP%"),
                new LevCheckbox(chkTurnOffFW, "netsh Advfirewall set allprofiles state off"),
                new LevCheckbox(chkDisableSleep, "powercfg /x -standby-timeout-ac 0"),
            });

            // Initialize LevCheckbox list for software
            if (Environment.Is64BitOperatingSystem) // 64 bit OS
            {
                levCheckbox4Software = new List<LevCheckbox>(new LevCheckbox[]
                {
                    new LevCheckbox(chkChrome, "https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7B162F372C-537B-5D4B-4170-3A63D3FA265F%7D%26lang%3Den%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26ap%3Dx64-stable-statsdef_1%26installdataindex%3Ddefaultbrowser/chrome/install/ChromeStandaloneSetup64.exe",
                                    "ChromeSetup64.exe", "ChromeSetup64.exe /silent /install"),
                    new LevCheckbox(chkCoccoc, "http://files.coccoc.com/browser/coccoc_standalone_vi.exe", "CocCocSetup.exe", "CocCocSetup.exe /silent /install"),
                    new LevCheckbox(chkUnikey, "http://file.lowendviet.com/Software/UniKey42RC.exe", "UniKey42RC.exe", " & copy " + Path.GetTempPath() + "UniKey42RC.exe " + Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                    new LevCheckbox(chkIDMSilient, "http://file.lowendviet.com/Software/IDM.60710.SiLeNt.InStAlL.exe", "IDMSilent.exe", "IDMSilent.exe /silent /install"),
                    new LevCheckbox(chkNotepad, "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.4.2/npp.8.4.2.Installer.x64.exe", "npp.exe", "npp.exe /S"),
                    new LevCheckbox(chkOpera, "https://ftp.opera.com/pub/opera/desktop/89.0.4447.71/win/Opera_89.0.4447.71_Setup_x64.exe", "OperaSetup.exe", "OperaSetup.exe /silent /install"),
                    new LevCheckbox(chkCcleaner, "https://download.ccleaner.com/ccsetup602.exe", "CCleaner.exe", "CCleaner.exe /S"),
                    new LevCheckbox(chk7zip, "https://www.7-zip.org/a/7z2201-x64.exe", "7zSetup.exe", "7zSetup.exe /S"),
                    new LevCheckbox(chkNET48, "https://download.visualstudio.microsoft.com/download/pr/014120d7-d689-4305-befd-3cb711108212/0fd66638cde16859462a6243a4629a50/ndp48-x86-x64-allos-enu.exe", "net48.exe", "net48.exe /q /norestart"),
                    new LevCheckbox(chkProxifier, "http://file.lowendviet.com/Software/Proxifier%203.21%20Setup.exe", "ProxifierSetup.exe", "ProxifierSetup.exe /S", "Use this key to register: KFZUS-F3JGV-T95Y7-BXGAS-5NHHP"),
                    new LevCheckbox(chkBitvise, "https://dl.bitvise.com/BvSshClient-Inst.exe", "BitviseSSH.exe", "BitviseSSH.exe -acceptEULA -force"),
                    new LevCheckbox(chkBrave, "https://laptop-updates.brave.com/latest/winx64", "Brave.exe", "Brave.exe /silent /install"),
                    new LevCheckbox(chkTor, "https://dist.torproject.org/torbrowser/11.5.1/torbrowser-install-win64-11.5.1_en-US.exe", "Tor.exe", "Tor.exe /S"),
                    new LevCheckbox(chkPutty, "https://the.earth.li/~sgtatham/putty/latest/w64/putty-64bit-0.77-installer.msi", "Putty64.exe", "msiexec /i Putty64.exe /quiet /qn"),
                    new LevCheckbox(chkCCProxy, "https://update.youngzsoft.com/ccproxy/update/ccproxysetup.exe", "ccproxysetup.exe", "ccproxysetup.exe /silent /install"),
                    new LevCheckbox(chk4K, "https://dl.4kdownload.com/app/4kvideodownloader_4.9.2_x64.msi?source=website", "4KDownloader.exe", "msiexec /i 4KDownloader.exe /quiet /qn"),
                    new LevCheckbox(chkUTorrent, "http://download-hr.utorrent.com/track/stable/endpoint/utorrent/os/windows", "uTorrent.exe", "uTorrent.exe"),
                    new LevCheckbox(chkBitTorrent, "https://www.bittorrent.com/downloads/complete/track/stable/os/win", "BitTorrent.exe", "BitTorrent.exe"),
                    new LevCheckbox(chkWinRAR, "https://www.rarlab.com/rar/winrar-x64-611.exe", "WinRAR.exe", "WinRAR.exe /s"),
                    new LevCheckbox(chkMwb, "https://data-cdn.mbamupdates.com/web/mb4-setup-consumer/MBSetup.exe", "MBSetup.exe", "MBSetup.exe /VERYSILENT /NORESTART"),
                    new LevCheckbox(chkImmunet, "https://download.immunet.com/binaries/immunet/bin/ImmunetSetup.exe", "ImmunetSetup.exe", "ImmunetSetup.exe /S"),
                });
            }
            else
            {
                levCheckbox4Software = new List<LevCheckbox>(new LevCheckbox[]
                {
                    new LevCheckbox(chkChrome, "https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7B6895D2F5-C00B-C0C3-5A9F-9F5A2D9AE003%7D%26lang%3Den%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26installdataindex%3Ddefaultbrowser/update2/installers/ChromeSetup.exe",
                                    "ChromeSetup.exe", "ChromeSetup.exe /silent /install"),
                    new LevCheckbox(chkCoccoc, "http://files.coccoc.com/browser/coccoc_standalone_vi.exe", "CocCocSetup.exe", "CocCocSetup.exe /silent /install"),
                    new LevCheckbox(chkUnikey, "http://file.lowendviet.com/Software/UniKey42RC.exe", "UniKey42RC.exe", " & copy " + Path.GetTempPath() + "UniKey42RC.exe " + Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                    new LevCheckbox(chkIDMSilient, "http://file.lowendviet.com/Software/IDM.60710.SiLeNt.InStAlL.exe", "IDMSilent.exe", "IDMSilent.exe /silent /install"),
                    new LevCheckbox(chkNotepad, "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.4.2/npp.8.4.2.Installer.exe", "npp.exe", "npp.exe /S"),
                    new LevCheckbox(chkOpera, "https://ftp.opera.com/pub/opera/desktop/89.0.4447.71/win/Opera_89.0.4447.71_Setup.exe", "OperaSetup.exe", "OperaSetup.exe /silent /install"),
                    new LevCheckbox(chkCcleaner, "https://download.ccleaner.com/ccsetup602.exe", "CCleaner.exe", "CCleaner.exe /S"),
                    new LevCheckbox(chk7zip, "https://www.7-zip.org/a/7z2201.exe", "7zSetup.exe", "7zSetup.exe /S"),
                    new LevCheckbox(chkNET48, "https://download.visualstudio.microsoft.com/download/pr/014120d7-d689-4305-befd-3cb711108212/0fd66638cde16859462a6243a4629a50/ndp48-x86-x64-allos-enu.exe", "net48.exe", "net48.exe /q /norestart"),
                    new LevCheckbox(chkProxifier, "http://file.lowendviet.com/Software/Proxifier%203.21%20Setup.exe", "ProxifierSetup.exe", "ProxifierSetup.exe /S", "Use this key to register: KFZUS-F3JGV-T95Y7-BXGAS-5NHHP"),
                    new LevCheckbox(chkBitvise, "https://dl.bitvise.com/BvSshClient-Inst.exe", "BitviseSSH.exe", "BitviseSSH.exe -acceptEULA -force"),
                    new LevCheckbox(chkBrave, "https://laptop-updates.brave.com/latest/winx64", "Brave.exe", "Brave.exe /silent /install"),
                    new LevCheckbox(chkTor, "https://dist.torproject.org/torbrowser/11.5.1/torbrowser-install-11.5.1_en-US.exe", "Tor.exe", "Tor.exe /S"),
                    new LevCheckbox(chkPutty, "https://the.earth.li/~sgtatham/putty/latest/w32/putty.exe", "Putty.exe", "msiexec /i Putty.exe /quiet /qn"),
                    new LevCheckbox(chkCCProxy, "https://update.youngzsoft.com/ccproxy/update/ccproxysetup.exe", "ccproxysetup.exe", "ccproxysetup.exe /silent /install"),
                    new LevCheckbox(chk4K, "https://dl.4kdownload.com/app/4kvideodownloader_4.9.2.msi?source=website", "4KDownloader.exe", "msiexec /i 4KDownloader.exe /quiet /qn"),
                    new LevCheckbox(chkUTorrent, "http://download-hr.utorrent.com/track/stable/endpoint/utorrent/os/windows", "uTorrent.exe", "uTorrent.exe"),
                    new LevCheckbox(chkBitTorrent, "https://www.bittorrent.com/downloads/complete/track/stable/os/win", "BitTorrent.exe", "BitTorrent.exe"),
                    new LevCheckbox(chkWinRAR, "https://www.rarlab.com/rar/winrar-x32-611.exe", "WinRAR.exe", "WinRAR.exe /s"),
                    new LevCheckbox(chkMwb, "https://data-cdn.mbamupdates.com/web/mb4-setup-consumer/MBSetup.exe", "MBSetup.exe", "MBSetup.exe /VERYSILENT /NORESTART"),
                    new LevCheckbox(chkImmunet, "https://download.immunet.com/binaries/immunet/bin/ImmunetSetup.exe", "ImmunetSetup.exe", "ImmunetSetup.exe /S"),
                });
            }
        }

        private void InitRegistry()
        {
            LEVStartupKey = Registry.LocalMachine.OpenSubKey(REG_STARTUP, true);
            LEVRegKey = Registry.CurrentUser.OpenSubKey(REG_LEV, true);

            // First time start
            if (LEVRegKey == null || LEVRegKey.GetValue("ForceChangePassword") == null ||
                LEVRegKey.GetValue("AutoUpdate") == null || LEVRegKey.GetValue("Version") == null)
            {
                LEVRegKey = Registry.CurrentUser.CreateSubKey(REG_LEV);
                LEVRegKey.SetValue("ForceChangePassword", "0"); // Default not require changing password
                LEVRegKey.SetValue("AutoUpdate", "1"); // Default auto update
                LEVStartupKey.SetValue(APPNAME, Application.ExecutablePath); // Default autostart
            }

            // Set version to Registry
            LEVRegKey.SetValue("Version", VERSION);

            // Load value for checkbox
            if (LEVRegKey.GetValue("ForceChangePassword").ToString() == "1")
            {
                this.chkForceChangePass.Checked = true;
            }
            if (LEVRegKey.GetValue("AutoUpdate").ToString() == "1")
            {
                this.chkUpdate.Checked = true;
            }
            if (LEVStartupKey.GetValue(APPNAME) != null)
            {
                this.chkStartUp.Checked = true;
            }
        }

        private void InitLEVDir()
        {
            try
            {
                // If the directory doesn't exist, create it.
                if (!Directory.Exists(LEV_DIR))
                {
                    Directory.CreateDirectory(LEV_DIR);
                }
            }
            catch (Exception)
            {
                // Fail silently
            }
        }

        private void LoadNetworkConfigFile(string filePath)
        {
            try
            {
                #region Load IPv4 configuration
                string fileContent = File.ReadAllText(filePath);
                fileContent = fileContent.TrimEnd('\r', '\n');
                fileContent = fileContent.Replace("\r\n\n", "\r\n");

                if (fileContent.IndexOf("\r\n\r\n") < 20) // Work around for network config file that was edited by a Linux editor like Nano...
                {
                    fileContent = fileContent.Replace("\r\n\r\n", "\r\n");
                }

                string[] lines = fileContent.Split(new string[] { "\r\n" }, StringSplitOptions.None);

                txtIP.Text = lines[0];
                txtNetmask.Text = lines[1];
                txtGateway.Text = lines[2];
                cmbDNS.SelectedIndex = cmbDNS.Items.Count - 1;
                if (lines.Length >= 4)
                {
                    txtCustomDNS.Text = lines[3];
                }
                #endregion

                #region Load IPv6 configuration
                if (lines.Length >= 7)
                {
                    txtIPV6.Text = lines[4];
                    txtNetmaskV6.Text = lines[5];
                    txtGatewayV6.Text = lines[6];
                    cbbDNSV6.SelectedIndex = cbbDNSV6.Items.Count - 1;
                }
                if (lines.Length >= 8)
                {
                    txtCustomDNSV6.Text = lines[7];
                }
                #endregion

            }
            catch
            {
                throw;
            }
        }

        private void SetupAutoLogin(string autoLoginPassword)
        {
            ExecuteCommand("REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v AutoAdminLogon /t REG_SZ /d 1 /f");
            //"REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v DefaultDomainName /t REG_SZ /d " + Environment.UserDomainName + " /f && " +
            ExecuteCommand("REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v DefaultUserName /t REG_SZ /d Administrator /f");
            ExecuteCommand("REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v DefaultPassword /t REG_SZ /d " + autoLoginPassword + " /f");
        }

        private bool changePassword(string account, string oldPassword, string newPassword)
        {
            if (newPassword.Length < 8 || !(newPassword.Any(char.IsUpper) && newPassword.Any(char.IsLower) && newPassword.Any(char.IsDigit)))
            {
                MessageBox.Show("New password must have at least 8 character, with both UPPERCASE, lowercase and number!", "Error!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                try
                {
                    DirectoryEntry oDE = new DirectoryEntry(string.Format(@"WinNT://" + Environment.MachineName + "/" + account + ",User"));

                    oDE.Invoke("ChangePassword", new object[] { oldPassword, newPassword });
                    MessageBox.Show("Successfully change Windows password!", "Success!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                catch (Exception)
                {
                    MessageBox.Show("WRONG password!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                // executeCommand("net user " + account + " \"" + newPassword + "\"", true);
            }

        }

        private static void SetIPv4Static(string ip, string netmask, string gateway, string dns)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    try
                    {
                        ManagementBaseObject setIP;
                        ManagementBaseObject newIP =
                            objMO.GetMethodParameters("EnableStatic");

                        newIP["IPAddress"] = new string[] { ip };
                        newIP["SubnetMask"] = new string[] { netmask };

                        setIP = objMO.InvokeMethod("EnableStatic", newIP, null);

                        ManagementBaseObject setGateway;
                        ManagementBaseObject newGateway =
                            objMO.GetMethodParameters("SetGateways");

                        newGateway["DefaultIPGateway"] = new string[] { gateway };
                        newGateway["GatewayCostMetric"] = new int[] { 1 };

                        setGateway = objMO.InvokeMethod("SetGateways", newGateway, null);

                        ManagementBaseObject newDNS =
                            objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        newDNS["DNSServerSearchOrder"] = dns.Split(',');
                        ManagementBaseObject setDNS =
                            objMO.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);

                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        private static void SetIPv4DHCP(string dns)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    try
                    {
                        ManagementBaseObject setDHCP;

                        setDHCP = objMO.InvokeMethod("EnableDHCP", null, null);

                        ManagementBaseObject newDNS =
                            objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        newDNS["DNSServerSearchOrder"] = dns.Split(',');
                        ManagementBaseObject setDNS =
                            objMO.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);

                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }


        private void ExtendDisk()
        {
            string textScript = "SELECT DISK 0" + Environment.NewLine
                    + "RESCAN" + Environment.NewLine
                    + "SELECT PARTITION 2" + Environment.NewLine
                    + "EXTEND" + Environment.NewLine
                    + "EXIT";
            File.WriteAllText(DISKPART_CONFIG_PATH, textScript);
            ExecuteCommand("diskpart.exe /s " + DISKPART_CONFIG_PATH, true);
            File.Delete(DISKPART_CONFIG_PATH);
        }

        private static int ExecuteCommand(string commnd, bool sync = false, int timeout = 200000)
        {
            var pp = new ProcessStartInfo("cmd.exe", "/C" + commnd)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = "C:\\",
            };
            var process = Process.Start(pp);
            if (sync == true)
            {
                process.WaitForExit(timeout);
                int exitCode = process.ExitCode;
                process.Close();
                return exitCode;

            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Start a <see cref="Process"/>
        /// </summary>
        /// <param name="processName">The <see cref="ProcessStartInfo.FileName"/> (usually name of the exe / command to start)</param>
        /// <param name="args">The <see cref="ProcessStartInfo.Arguments"/> (the argument of the command)</param>
        /// <param name="verb">The <see cref="ProcessStartInfo.Verb"/>. Use "runas" to start the process with admin priviledge (default is null)</param>
        /// <param name="useShell">The <see cref="ProcessStartInfo.UseShellExecute"/>. Does the process run silently or not (silent by default)</param>
        /// <param name="redirectErros">The <see cref="ProcessStartInfo.RedirectStandardError"/>. Do we redirect standard error ? (true by default)</param>
        /// <param name="redirectOutput">The <see cref="ProcessStartInfo.RedirectStandardOutput"/>. Do we redirect standard output ? (true by default)</param>
        /// <param name="noWindow">The <see cref="ProcessStartInfo.CreateNoWindow"/>. Do we prevent the creation of CMD window to run silently ? (silent by default)</param>
        /// <returns>True if <paramref name="processName"/> isn't null and process execution succeeded. False if <paramref name="processName"/> is null or empty.
        /// Throw an <see cref="Exception"/> if execution failed</returns>
        private static string StartProcess(string processName, string args, string verb = null, bool useShell = false, bool redirectErros = true, bool redirectOutput = true, bool noWindow = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = processName;
            psi.Arguments = args;
            psi.UseShellExecute = useShell;
            psi.RedirectStandardOutput = redirectOutput;
            psi.RedirectStandardError = redirectErros;
            psi.CreateNoWindow = noWindow;
            if (verb != null)
                psi.Verb = verb;
            Process proc = Process.Start(psi);
            proc.WaitForExit();
            string errors = proc.StandardError.ReadToEnd();
            string output = proc.StandardOutput.ReadToEnd();
            if (proc.ExitCode != 0)
                throw new Exception(processName + " exit code: " + proc.ExitCode.ToString() + " " + (!string.IsNullOrEmpty(errors) ? " " + errors : "") + " " + (!string.IsNullOrEmpty(output) ? " " + output : ""));
            return output;
        }

        /// <summary>
        /// Convinient method to start a "netsh" process as admin to set a new DNS IP address calling <see cref="netshSetNewDNS(string, string, bool)"/>
        /// </summary>
        /// <param name="interfaceName">The name of the interface to set its new <paramref name="address"/> IP ddress</param>
        /// <param name="address">The new IP address to set of the <paramref name="interfaceName"/> DNS</param>
        /// <param name="isPrimary">Is this new DNS IP address is a primary one ?</param>
        /// <param name="isIPv6">Does <paramref name="address"/> is IPv6 ?</param>
        /// <returns><see cref="netshSetNewDNS(string, string, bool)"/> return value</returns>
        private static bool NetshSetNewDNS(string interfaceName, string address, bool isPrimary, bool isIPv6)
        {
            string arg = string.Format("interface {0} {1} dnsservers \"{2}\"{3} {4} {5}", isIPv6 ? "ipv6" : "ipv4", isPrimary ? "set" : "add", interfaceName, isPrimary ? " static" : "", address, isPrimary ? "primary" : "index=2");
            StartProcess("netsh", arg, "runas");
            return true;
        }

        /// <summary>
        /// Method to set the DNS IP addresses of a given <see cref="NetworkInterface"/>
        /// note : we use netsh silently.
        /// see : https://www.tenforums.com/tutorials/77444-change-ipv4-ipv6-dns-server-address-windows.html
        /// </summary>
        /// <param name="ni">The <see cref="NetworkInterface"/> adapter to modify its DNS IP addresses along given <paramref name="addresses"/></param>
        /// <param name="ipv4">The IPv4 addresses to store in <paramref name="ni"/> adapter as its new DNS IP addresses</param>
        /// <param name="ipv6">The IPv6 addresses to store in <paramref name="ni"/> adapter as its new DNS IP addresses</param>
        private static void SetIPv6DNS(string ipv6DnsAddress)
        {
            var CurrentInterface = GetActiveEthernetOrWifiNetworkInterface();
            if (CurrentInterface == null) return;

            // delete current IPv6 DNS
            StartProcess("netsh", "interface ipv6 delete dnsservers \"" + CurrentInterface.Name + "\" all", "runas");

            //set new IPv6 DNS addresses
            NetshSetNewDNS(CurrentInterface.Name, ipv6DnsAddress, true, true);
        }

        private static void ClearIPv6(NetworkInterface networkInterface)
        {
            try
            {
                var args = "netsh interface ipv6 show addresses interface=\"" + networkInterface.Name + "\" level=verbose | findstr Parameters";
                var resultCmd = StartProcess("cmd.exe", "/c " + args, "runas");
                var iPv6List = getCurrentIPv6List(networkInterface);

                // delete current IPv6
                foreach (var iPv6 in iPv6List)
                {
                    args = $"netsh interface ipv6 delete address \"{networkInterface.Name}\" {iPv6}";
                    StartProcess("cmd.exe", "/c " + args, "runas");
                }

                // get current gateway
                args = $"netsh interface ipv6 show route | findstr \"::/0\"";
                resultCmd = StartProcess("cmd.exe", "/c " + args, "runas");
                var gatewatList = resultCmd.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(' ')[38]).Where(x => !x.StartsWith("fe80") && !string.IsNullOrWhiteSpace(x)).ToArray();

                // delete current gateway
                foreach (var gateway in gatewatList)
                {
                    args = $"route delete ::/0 {gateway}";
                    StartProcess("cmd.exe", "/c " + args, "runas");
                }
            } catch (Exception ex)
            {
                // Muted exception
            }

        }
        private static void SkipSourceIPv6(string iPv6, NetworkInterface networkInterface, string skipSourceValue)
        {
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddScript($"Set-NetIPAddress -IPAddress '{iPv6}' -InterfaceAlias  '{networkInterface.Name}' -SkipAsSource ${skipSourceValue}");
                powerShell.Invoke();
                if (powerShell.HadErrors)
                {
                    // Siliently ignore
                    return;
                }
                return;
            }
        }

        private static void SetIPv6DHCP(NetworkInterface networkInterface, bool isEnable)
        {
            string dhcpMode = isEnable ? "enabled" : "disabled";
            var args = $"netsh int ipv6 set int \"{networkInterface.Name}\" routerdiscovery={dhcpMode}";
            StartProcess("cmd.exe", "/c " + args, "runas");
            if (!isEnable)
            {
                args = args = $"netsh int ipv6 set int \"{networkInterface.Name}\" managedaddress={dhcpMode}";
                StartProcess("cmd.exe", "/c " + args, "runas");
            }
        }

        private static void SetIPv6Static(NetworkInterface networkInterface, string iPv6, string subnetPrefixLength, string gateway)
        {
            var iPv6List = getCurrentIPv6List(networkInterface);
            if (iPv6List.Contains(iPv6))
            {
                SkipSourceIPv6(iPv6, networkInterface, "false");
            }
            else
            {
                var args = $"netsh interface ipv6 add address \"{networkInterface.Name}\" {iPv6}/{subnetPrefixLength}";
                StartProcess("cmd.exe", "/c " + args, "runas");
                args = $"netsh interface ipv6 add route ::/0 \"{networkInterface.Name}\" {gateway}";
                StartProcess("cmd.exe", "/c " + args, "runas");
            }
        }


        private static void DisableIPv6(NetworkInterface networkInterface)
        {
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddScript($"Disable-NetAdapterBinding -Name '{networkInterface.Name}' -ComponentID ms_tcpip6");
                powerShell.Invoke();
                if (powerShell.HadErrors)
                {
                    // Failed, do something
                    return;
                }
                // Success, do something
                return;
            }
        }

        private void WriteConfigFile()
        {
            string config = txtIP.Text + Environment.NewLine
                            + txtNetmask.Text + Environment.NewLine
                            + txtGateway.Text + Environment.NewLine
                            + txtCustomDNS.Text + Environment.NewLine
                            + txtIPV6.Text + Environment.NewLine
                            + txtNetmaskV6.Text + Environment.NewLine
                            + txtGatewayV6.Text + Environment.NewLine
                            + txtCustomDNSV6.Text + Environment.NewLine
                            + Environment.NewLine; //card network;
            try
            {
                File.WriteAllText(NETWORK_CONFIG_PATH, config);
            }
            catch
            {
                throw;
            }
        }

        private static bool IsIPv6Enable()
        {
            var args = "ipconfig";
            var cmdOutput = StartProcess("cmd.exe", "/c " + args, "runas");
            return cmdOutput.ToLower().Contains("ipv6");
        }
        private static NetworkInterface GetActiveEthernetOrWifiNetworkInterface()
        {
            try
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                    a => a.OperationalStatus == OperationalStatus.Up &&
                    (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                    a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"));
                if (nic == null)
                {
                    nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                        a => a.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        a.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
                }
                if (nic == null)
                {
                    nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                        a => a.OperationalStatus == OperationalStatus.Up);
                }
                return nic;
            }
            catch
            {
                return null;
            }
        }

        private static string[] getCurrentIPv6List (NetworkInterface networkInterface)
        {

            var args = "netsh interface ipv6 show addresses interface=\"" + networkInterface.Name + "\" level=verbose | findstr Parameters";

            
            var resultCmd = StartProcess("cmd.exe", "/c " + args, "runas");
            var iPv6List = resultCmd.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(' ')[1]).Where(x => !x.StartsWith("fe80")).ToArray();
            
            return iPv6List;

        }

        private string GenerateRandomIPv6Quartet(int numberOfQuartet)
        {
            string result = "";
            const string chars = "0123456789abcdef";
            Random random = new Random();
            for (int i = 0; i < numberOfQuartet; i ++)
            {
                result = result + new string(Enumerable.Repeat(chars, 4)
                    .Select(s => s[random.Next(s.Length)]).ToArray()) + ':';
            }
            return result.TrimEnd(':');
        }

        private string ConvertIPv6ToFullFormat(string currentIPv6)
        {
            string[] ipv6Parts = currentIPv6.Split(new string[] { "::" }, StringSplitOptions.None);
            string ipv6FullFormat = ipv6Parts[0];

            if (ipv6Parts.Length == 2)
            {
                var zeroPartLength = 8 - ipv6Parts[0].Split(new string[] { ":" }, StringSplitOptions.None).Length
                                       - ipv6Parts[1].Split(new string[] { ":" }, StringSplitOptions.None).Length;
                for (int i = 0; i < zeroPartLength; i++)
                {
                    ipv6FullFormat = ipv6FullFormat + ":0000";
                }
                ipv6FullFormat = ipv6FullFormat + ":" + ipv6Parts[1];
            }
            var ipv6QuartetList = ipv6FullFormat.Split(new string[] { ":" }, StringSplitOptions.None);
            if (ipv6QuartetList.Length < 8)
            {
                return null;
            }
            else
            {
                return ipv6FullFormat;
            }
        }
        #endregion Private functions

        #region Inner classes

        public class DNSConfig
        {
            public string serverName { get; set; }
            public string DNS1 { get; set; }

            public DNSConfig(string serverName, string DNS1)
            {
                this.serverName = serverName;
                this.DNS1 = DNS1;
            }

            public override string ToString()
            {
                return this.serverName + " | " + this.DNS1;
            }
        }
        #endregion
    }
}
