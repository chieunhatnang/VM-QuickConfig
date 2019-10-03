using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace LowEndViet.com_VPS_Tool
{
    public partial class form_LowEndVietFastVPSConfig : Form
    {
        #region Final variables
        static readonly string APPNAME = "VM QuickConfig";
        public readonly string VERSION = "1.2";
        static readonly string GITHOME = "abc";

        static readonly string REGSTARTUP = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\";
        static readonly string REGLEV = "Software\\LEV\\VMQuickConfig";
        static readonly string DISKPARTCONFIGPATH = "C:\\Users\\Public\\LEV\\diskpartconfig.txt";

        #endregion

        #region Global variables
        public static List<DNSConfig> DNSServerList;

        public List<LevCheckbox> levCheckbox4WindowsList;
        public List<LevCheckbox> levCheckbox4Software;

        IPConfig ipConfig = null;
        RegistryKey LEVStartupKey;
        RegistryKey LEVRegKey;
        #endregion

        public form_LowEndVietFastVPSConfig(string [] args)
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.Text = this.Text + " Version " + VERSION;

            this.lnkGit.Text = GITHOME;

            initCheckbox();
            initRegistry();

            // Initialize combobox
            foreach (DNSConfig dnsConfig in DNSServerList)
            {
                this.cmbDNS.Items.Add(dnsConfig);
            }
            cmbDNS.DropDownWidth = 200;

            //Set the tooltip
            ttAutologin.SetToolTip(label8, "If you check this box, your VPS will be automatically login when it is started.\r\n" +
                "It allows you to reset your password over Web console in case you forget the password.");
        
        }

        #region Event Processing
        private void form_LowEndVietFastVPSConfig_Load(object sender, EventArgs e)
        {
            // Check and force change password
            if (LEVRegKey.GetValue("ForceChangePassword").ToString() == "1")
            {
                executeCommand("taskkill /IM explorer.exe /F", true);
                string newPassword = "";
                ForcePasswordChange frm = new ForcePasswordChange();
                var formResult = frm.ShowDialog();
                if (formResult == DialogResult.OK)
                {
                    executeCommand("explorer.exe");
                    newPassword = frm.newPassword;
                    changePassword(newPassword);
                    setupAutoLogin(newPassword);
                    LEVRegKey.SetValue("ForceChangePassword", 0);
                    chkForceChangePass.Checked = false;
                }
            }
        }


        private void rdDHCP_CheckedChanged(object sender, EventArgs e)
        {
            if (rdStatic.Checked)
            {
                txtIP.Enabled = true;
                txtNetmask.Enabled = true;
                txtGateway.Enabled = true;
            }
            if (rdDHCP.Checked)
            {
                txtIP.Enabled = false;
                txtNetmask.Enabled = false;
                txtGateway.Enabled = false;
            }
        }

        private void bntConfigNetwork_Click(object sender, EventArgs e)
        {
            if (rdStatic.Checked)
            {
                setStaticIP(txtIP.Text, txtNetmask.Text, txtGateway.Text, txtCustomDNS.Text);
            }
            if (rdDHCP.Checked)
            {
                setDHCP(txtCustomDNS.Text);
            }
            MessageBox.Show("Successfully set your network configuration!", "Success!",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void cmbDNS_SelectedIndexChanged(object sender, EventArgs e)
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
     

        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            changePassword(txtNewPassword.Text);
            if (chkAutoLogin.Checked)
            {
                setupAutoLogin(txtNewPassword.Text);
            }
        }


        private void btnExtendDisk_Click(object sender, EventArgs e)
        {
            extendDisk();
            MessageBox.Show("Successfully extend your disk to maximum capacity!", "Success!",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void btnCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach(LevCheckbox levCheckbox in levCheckbox4WindowsList)
            {
                levCheckbox.checkBox.Checked = chkCheckAll.Checked;
            }
        }

        private void btnConfigWindows_Click(object sender, EventArgs e)
        {
            StatusForm statusForm = new StatusForm(levCheckbox4WindowsList);
            statusForm.Show();
            Thread t = new Thread(() =>
            {
                foreach (LevCheckbox levCheckbox in levCheckbox4WindowsList)
                {
                    if (levCheckbox.checkBox.Checked)
                    {
                        levCheckbox.updateResultStatus(executeCommand(levCheckbox.command, true));
                        statusForm.updateProgress();
                    }
                }

                executeCommand("taskkill /IM explorer.exe /F & explorer.exe", true);
            });
            t.Start();
        }


        private void btnInstall_Click(object sender, EventArgs e)
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
            Thread t = new Thread(() =>
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
                        catch(Exception ex)
                        {
                            levCheckbox.remark = ex.Message;
                        }
                        finally
                        {
                            levCheckbox.updateInstallingStatus();
                        }
                        statusForm.updateProgress();
                        levCheckbox.updateResultStatus(executeCommand(Path.GetTempPath() + levCheckbox.command, true));
                        statusForm.updateProgress();
                    }
                }
                wc.Dispose();
            });
            t.Start();
        }

        private void chkStartUp_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStartUp.Checked == true)
            {
                LEVStartupKey.SetValue(APPNAME, Application.ExecutablePath);
            } else
            {
                LEVStartupKey.DeleteValue(APPNAME, false);
            }
        }

        private void chkUpdate_CheckedChanged(object sender, EventArgs e)
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


        private void chkForceChangePass_CheckedChanged(object sender, EventArgs e)
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

        private void form_LowEndVietFastVPSConfig_FormClosed(object sender, FormClosedEventArgs e)
        {
            LEVStartupKey.Close();
            LEVRegKey.Close();
        }

        #endregion

        #region Private functions
        private void initCheckbox()
        {
            DNSServerList = new List<DNSConfig>(new DNSConfig[] {
            new DNSConfig("Google DNS", "8.8.8.8"),
            new DNSConfig("Cloudflare DNS", "1.1.1.1"),
            new DNSConfig("Cisco OpenDNS", "208.67.222.222"),
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
                    new LevCheckbox(chkUnikey, "http://file.levnode.com/Software/UniKey42RC.exe", "UniKey42RC.exe", "copy UniKey42RC.exe " + Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                    new LevCheckbox(chkIDMSilient, "http://file.levnode.com/Software/IDM.60710.SiLeNt.InStAlL.exe", "IDMSilent.exe", "IDMSilent.exe /silent /install"),
                    new LevCheckbox(chkNotepad, "https://notepad-plus-plus.org/repository/7.x/7.5.6/npp.7.5.6.Installer.exe", "npp.exe", "npp.exe /S"),
                    new LevCheckbox(chkOpera, "https://ftp.opera.com/pub/opera/desktop/53.0.2907.37/win/Opera_53.0.2907.37_Setup.exe", "OperaSetup.exe", "OperaSetup.exe /silent /install"),
                    new LevCheckbox(chkCcleaner, "https://download.ccleaner.com/ccsetup542.exe", "CCleaner.exe", "CCleaner.exe /S"),
                    new LevCheckbox(chk7zip, "https://www.7-zip.org/a/7z1900-x64.exe", "7zSetup.exe", "7zSetup.exe /S"),
                    new LevCheckbox(chkNET48, "https://download.visualstudio.microsoft.com/download/pr/014120d7-d689-4305-befd-3cb711108212/0fd66638cde16859462a6243a4629a50/ndp48-x86-x64-allos-enu.exe", "net48.exe", "net48.exe /q /norestart"),
                    new LevCheckbox(chkProxifier, "http://file.levnode.com/Software/Proxifier%203.21%20Setup.exe", "ProxifierSetup.exe", "ProxifierSetup.exe /S", "Use this key to register: KFZUS-F3JGV-T95Y7-BXGAS-5NHHP"),
                    new LevCheckbox(chkBitvise, "https://dl.bitvise.com/BvSshClient-Inst.exe", "BitviseSSH.exe", "BitviseSSH.exe -acceptEULA -force"),
                    new LevCheckbox(chkBrave, "https://laptop-updates.brave.com/latest/winx64", "Brave.exe", "Brave.exe /silent /install"),
                    new LevCheckbox(chkTor, "https://www.torproject.org/dist/torbrowser/8.5.5/torbrowser-install-win64-8.5.5_en-US.exe", "Tor.exe", "Tor.exe /S"),
                    new LevCheckbox(chkPutty, "https://the.earth.li/~sgtatham/putty/latest/w64/putty-64bit-0.73-installer.msi", "Putty64.exe", "msiexec /i Putty64.exe /quiet /qn"),
                    new LevCheckbox(chk4K, "https://dl.4kdownload.com/app/4kvideodownloader_4.9.2_x64.msi?source=website", "4KDownloader.exe", "msiexec /i 4KDownloader.exe /quiet /qn"),
                    new LevCheckbox(chkUTorrent, "http://download-hr.utorrent.com/track/stable/endpoint/utorrent/os/windows", "uTorrent.exe", "uTorrent.exe"),
                    new LevCheckbox(chkBitTorrent, "https://www.bittorrent.com/downloads/complete/track/stable/os/win", "BitTorrent.exe", "BitTorrent.exe"),
                    new LevCheckbox(chkWinRAR, "https://www.rarlab.com/rar/winrar-x64-58b2.exe", "WinRAR.exe", "WinRAR.exe /s"),
                });
            } else
            {
                levCheckbox4Software = new List<LevCheckbox>(new LevCheckbox[]
                {
                    new LevCheckbox(chkChrome, "https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7B6895D2F5-C00B-C0C3-5A9F-9F5A2D9AE003%7D%26lang%3Den%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26installdataindex%3Ddefaultbrowser/update2/installers/ChromeSetup.exe",
                                    "ChromeSetup.exe", "ChromeSetup.exe /silent /install"),
                    new LevCheckbox(chkCoccoc, "http://files.coccoc.com/browser/coccoc_standalone_vi.exe", "CocCocSetup.exe", "CocCocSetup.exe /silent /install"),
                    new LevCheckbox(chkUnikey, "http://file.levnode.com/Software/UniKey42RC.exe", "UniKey42RC.exe", "copy UniKey42RC.exe " + Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                    new LevCheckbox(chkIDMSilient, "http://file.levnode.com/Software/IDM.60710.SiLeNt.InStAlL.exe", "IDMSilent.exe", "IDMSilent.exe /silent /install"),
                    new LevCheckbox(chkNotepad, "https://notepad-plus-plus.org/repository/7.x/7.5.6/npp.7.5.6.Installer.exe", "npp.exe", "npp.exe /S"),
                    new LevCheckbox(chkOpera, "https://ftp.opera.com/pub/opera/desktop/53.0.2907.37/win/Opera_53.0.2907.37_Setup.exe", "OperaSetup.exe", "OperaSetup.exe /silent /install"),
                    new LevCheckbox(chkCcleaner, "https://download.ccleaner.com/ccsetup542.exe", "CCleaner.exe", "CCleaner.exe /S"),
                    new LevCheckbox(chk7zip, "https://www.7-zip.org/a/7z1900.exe", "7zSetup.exe", "7zSetup.exe /S"),
                    new LevCheckbox(chkNET48, "https://download.visualstudio.microsoft.com/download/pr/014120d7-d689-4305-befd-3cb711108212/0fd66638cde16859462a6243a4629a50/ndp48-x86-x64-allos-enu.exe", "net48.exe", "net48.exe /q /norestart"),
                    new LevCheckbox(chkProxifier, "http://file.levnode.com/Software/Proxifier%203.21%20Setup.exe", "ProxifierSetup.exe", "ProxifierSetup.exe /S", "Use this key to register: KFZUS-F3JGV-T95Y7-BXGAS-5NHHP"),
                    new LevCheckbox(chkBitvise, "https://dl.bitvise.com/BvSshClient-Inst.exe", "BitviseSSH.exe", "BitviseSSH.exe -acceptEULA -force"),
                    new LevCheckbox(chkBrave, "https://laptop-updates.brave.com/latest/winx64", "Brave.exe", "Brave.exe /silent /install"),
                    new LevCheckbox(chkTor, "https://dl.bitvise.com/BvSshClient-Inst.exe", "Tor.exe", "Tor.exe /S"),
                    new LevCheckbox(chkPutty, "https://the.earth.li/~sgtatham/putty/latest/w32/putty-0.73-installer.msi", "Putty.exe", "msiexec /i Putty.exe /quiet /qn"),
                    new LevCheckbox(chk4K, "https://dl.4kdownload.com/app/4kvideodownloader_4.9.2.msi?source=website", "4KDownloader.exe", "msiexec /i 4KDownloader.exe /quiet /qn"),
                    new LevCheckbox(chkUTorrent, "http://download-hr.utorrent.com/track/stable/endpoint/utorrent/os/windows", "uTorrent.exe", "uTorrent.exe"),
                    new LevCheckbox(chkBitTorrent, "https://www.bittorrent.com/downloads/complete/track/stable/os/win", "BitTorrent.exe", "BitTorrent.exe"),
                    new LevCheckbox(chkWinRAR, "https://www.rarlab.com/rar/wrar58b2.exe", "WinRAR.exe", "WinRAR.exe /s"),
                });
            }
        }

        private void initRegistry ()
        {
            LEVStartupKey = Registry.LocalMachine.OpenSubKey(REGSTARTUP, true);
            LEVRegKey = Registry.CurrentUser.OpenSubKey(REGLEV, true);

            // First time start
            if (LEVRegKey == null || LEVRegKey.GetValue("ForceChangePassword") == null ||
                LEVRegKey.GetValue("AutoUpdate") == null || LEVRegKey.GetValue("Version") == null)
            {
                LEVRegKey = Registry.CurrentUser.CreateSubKey(REGLEV);
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

        private void setupAutoLogin(string autoLoginPassword)
        {
            executeCommand("REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v AutoAdminLogon /t REG_SZ /d 1 /f");
            //"REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v DefaultDomainName /t REG_SZ /d " + Environment.UserDomainName + " /f && " +
            executeCommand("REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v DefaultUserName /t REG_SZ /d Administrator /f");
            executeCommand("REG ADD \"HKLM\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon\" /v DefaultPassword /t REG_SZ /d " + autoLoginPassword + " /f");
        }

        private void changePassword(string newPassword)
        {
            if (newPassword.Length < 8 || !(newPassword.Any(char.IsUpper) && newPassword.Any(char.IsLower) && newPassword.Any(char.IsDigit)))
            {
                MessageBox.Show("New password must have at least 8 character, with both UPPERCASE, lowercase and number!", "Error!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }
            else
            {
                executeCommand("net user Administrator \"" + newPassword + "\"", true);
                MessageBox.Show("Successfully change Windows password!", "Success!",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        
        }

        private static void setStaticIP(string ip, string netmask, string gateway, string dns)
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

        private static void setDHCP(string dns)
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

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    using (var stream = client.OpenRead("https://google.com"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static string getLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        private void extendDisk()
        {
            string textScript = "SELECT DISK 0" + Environment.NewLine
                    + "RESCAN" + Environment.NewLine
                    + "SELECT PARTITION 2" + Environment.NewLine
                    + "EXTEND" + Environment.NewLine
                    + "EXIT";
            File.WriteAllText(DISKPARTCONFIGPATH, textScript);
            executeCommand("diskpart.exe /s " + DISKPARTCONFIGPATH, true);
            File.Delete(DISKPARTCONFIGPATH);
        }

        private static int executeCommand(string commnd, bool sync = false, int timeout = 200000)
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

        #endregion Private functions

        #region Inner classes
        class IPConfig
        {
            public string ip { get; set; }
            public string netmask { get; set; }
            public string gateway { get; set; }
            public DNSConfig dns { get; set; }

            public IPConfig(string filePath)
            {
                string fileContent = File.ReadAllText(filePath);
                string[] lines = fileContent.Split(Environment.NewLine.ToCharArray());
                this.ip = lines[0];
                this.netmask = lines[1];
                this.gateway = lines[2];
                if (lines.Length > 3)
                {
                    this.dns = new DNSConfig("Custom DNS", lines[3]);
                }
                else
                {
                    this.dns = new DNSConfig("Google DNS", "8.8.8.8");
                }
            }
            public IPConfig()
            {
                this.ip = "";
                this.netmask = "";
                this.gateway = "";
                this.dns = new DNSConfig("Google DNS", "8.8.8.8");
            }
        }

        public class DNSConfig
        {
            public string serverName { get; set; }
            public string DNS1 { get; set; }

            public DNSConfig (string serverName, string DNS1)
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
