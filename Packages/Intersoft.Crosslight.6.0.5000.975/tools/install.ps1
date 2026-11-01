param($installPath, $toolsPath, $package, $project)

$enableLog = $False

if ($enableLog)
{
    Write-Warning ("Install Path : " + $installPath)
    Write-Warning ("Tools Path : " + $toolsPath)
    Write-Warning ("Project : " + $project.Name)
    Write-Warning ("Project File Name: " + $project.FileName)
}

$libPath = [System.IO.Path]::Combine($toolsPath, "NugetUtil.dll")

if ($enableLog)
{
    Write-Warning ("NugetUtil Path : " + $libPath)
}

[System.Reflection.Assembly]::LoadFile($libPath) | Out-Null
$packages = [NugetUtil.PackageManager]::GetDependencies($toolsPath, $project.FileName)

foreach($package in $packages)
{	
	Write-Warning $package.Name
	Write-Warning $package.Version
	Write-Warning $package.Pre
	
	if ($package.Version -eq "")
	{
		if ($package.Pre -eq "")
		{
            if ($enableLog)
            {
			    Write-Warning "package.Name -project project.Name"
            }

			Install-Package $package.Name -project $project.Name
		}
		else
		{
			if ($enableLog)
            {
			    Write-Warning "package.Name -project project.Name -pre"
            }
			Install-Package $package.Name -project $project.Name -pre
		}
	}
	else
	{
		if ($package.Pre -eq "")
		{		
			if ($enableLog)
            {
			    Write-Warning "package.Name -project -version project.Name"
            }
			Install-Package $package.Name -version $package.Version -project $project.Name
		}
		else
		{
		
			if ($enableLog)
            {
			    Write-Warning "package.Name -project -version project.Name -pre"
            }
			Install-Package $package.Name -version $package.Version -project $project.Name -pre
		}
	}
}
      
      
	 

