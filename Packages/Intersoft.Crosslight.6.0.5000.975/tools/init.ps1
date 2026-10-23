param($installPath, $toolsPath, $package, $project)

$enableLog = $False

if ($enableLog)
{
    Write-Warning ("Install Path : " + $installPath)
    Write-Warning ("Tools Path : " + $toolsPath)
}

$os = [Environment]::OSVersion.VersionString.SubString(0,4)
$cocoaDialogSh = [System.IO.Path]::Combine($toolsPath, "CocoaDialog.sh")
$installLog = [System.IO.Path]::Combine($toolsPath, "install.log")
$hasInstallLog = [System.IO.File]::Exists($installLog)

$first = "true"
if ($hasInstallLog)
{
	$first = "false"
}

if ($enableLog)
{
    Write-Warning ("OS : " + $os)
    Write-Warning ("CocoaDialog.sh : " + $cocoaDialogSh)
    Write-Warning ("Install Log : " + $installLog)
    Write-Warning ("Has Install Log : " + $cocoaDialogSh)
    Write-Warning ("First Installation : " + $first)
}

if ($os -eq "Unix") 
{
    $filePath = "/Library/Application Support/Intersoft Solutions/crosslight.info"
    $cocoaDialogZip = [System.IO.Path]::Combine($toolsPath, "CocoaDialog.app.zip")
    $cocoaDialogApp = [System.IO.Path]::Combine($toolsPath, "CocoaDialog.app/Contents/MacOS/CocoaDialog")
    
    if ($enableLog)
    {
        Write-Warning ("Crosslight.info : " + $filePath)
        Write-Warning ("CocoaDialog.zip : " + $cocoaDialogZip)
        Write-Warning ("CocoaDialog.app : " + $cocoaDialogApp)
        Write-Warning "Unzipping CocoaDialog.zip..."
    }

    [System.Diagnostics.Process]::Start("unzip", $cocoaDialogZip + " -d " + $toolsPath) | Out-Null
    
    if ($enableLog)
    {
        Write-Warning "Unzipping CocoaDialog.zip (Done)..."
    }
}
else
{
    $platform=Get-WMIObject win32_operatingsystem
    if ($platform.OSArchitecture -eq "64-bit") 
    {
      $intersoftKey = "hklm:\SOFTWARE\Wow6432Node\Intersoft Solutions\Crosslight"
    } 
    else 
    {
      $intersoftKey = "hklm:\SOFTWARE\Intersoft Solutions\Crosslight"
    }

    if (Test-Path $intersoftKey)
    {
      $installFolderKey = Get-ItemProperty -Path $intersoftKey -Name "InstallFolder"
      $filePath = join-path -path $installFolderKey.InstallFolder -childPath "crosslight.info"
    }

    if ($enableLog)
    {
        Write-Warning ("Platform : " + $platform.OSArchitecture)
        Write-Warning ("Intersoft Key : " + $intersoftKey)
        Write-Warning ("Install Folder Key : " + $installFolderKey)
        Write-Warning ("File Path : " + $filePath)
    }

}

$warningMessage = "Crosslight is not installed in this machine. For best development experience such as IDE integration and templates, it is highly recommended to download and install Crosslight full setup to this machine.

Would you like to do this now?

Note: If you choose No, your packages will still be installed properly and all runtime features should work properly."

if ($first -eq "true")
{
	if ([System.IO.File]::Exists($filePath)) 
	{
		$file = [System.IO.File]::ReadAllText($filePath)

        if ($enableLog)
        {
            Write-Warning "Crosslight is already installed."
        }
	}
	else
	{
        if ($enableLog)
        {
            Write-Warning "Crosslight is not installed."
        }

		if ($os -eq "Unix") 
		{
		  $result = [System.Diagnostics.Process]::Start("bash" ,$cocoaDialogSh + " " + $cocoaDialogApp) | Out-Null
		}
		else
		{
		  [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms") | Out-Null
		  $result = [Windows.Forms.MessageBox]::Show($warningMessage, "Crosslight is not installed", [Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Information)
		  if ($result -eq "YES" ) 
		  {
			[System.Diagnostics.Process]::Start("http://www.intersoftsolutions.com/RequestTrial")
		  }
		}
	}
	
	[System.IO.File]::Copy($cocoaDialogSh, $installLog)
}