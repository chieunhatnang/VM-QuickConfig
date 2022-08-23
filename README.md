# VM-QuickConfig

## Quick and easy configuration tool for Windows VPS.

You will find almost every settings you need when you setup your VPS at the first time in this tool.
This tool will need .NET Framework 4.0 or newer to run.

---
## Feature
### Network config: 
- Static IP/DHCP for IPv4/IPv6
- IP/Netmask/Gateway/DNS for IPv4/IPv6
### Security tool
- You can change the default Windows Administrator account to a different account to make it more secure.
- You can open TCP incoming port on firewall.
- You can change the RDP port to other port other than 3389.
- You can change the Windows password without knowing the old password. *_Note_*: The new password must be at least 8 characters and contains both UPPERCASE, lowercase and number.
### Disk extender tool
- If you resize your disk of the VM on the host, Windows may not recognize the new extended part. This tool will help you extend the disk to full capacity.
### Windows optimization
Some optimizations and settings that you may need:
- Disable User Account Control (UAC): Windows will not ask when you install new software/change system settings anymore.
- Turn off Internet Explorer ESC:
- Disable Windows update
- Show all icon on the tray
- Turn off firewall
- Disable sleep
- Disable hiberfil.sys file to save the disk
- Disable recovery at logon when Windows shutdown unexpectedly
- Disable driver signature to allow you install driver from third-party provider
- Optimizing Remote Desktop Protocol for smoother remote
- Delete temporary folder
- Setting time zone.
### Easy software installing tool
Some popular softwares are integrated for easier install. They are set to silent install so they will not ask any questions.
#### Browsers
- Google Chrome
- Firefox
- Opera
- Brave
- Cốc cốc
- Tor
#### SSH and IP faking tool
- Proxifier
- Bitvise Tunnelier
- Putty
- CCProxy
#### Download tool
- Internet download manager
- 4K Video downloader
- uTorrent
- bitTorrent
#### Utilities
- Unikey
- Notepad ++
- CCleaner
- Microsoft .NET framework 4.8
- 7zip
- WinRAR
## Static linking build
By default, VMQuickConfig buid will create the 2 following DLL files in the Releases folder: 
- Microsoft.Management.Infrastructure.dll
- System.Management.Automation.dll

If you want to import all DLL files in to a single executable files(static linking), run the batch script: mergedll.bat. The merged executable files will be put in the /export folder.
