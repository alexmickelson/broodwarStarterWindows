$ErrorActionPreference = "Stop"

function Download-StarCraft {
    param(
        [string]$Url,
        [string]$ZipPath,
        [string]$Destination
    )
    
    Write-Host "Downloading StarCraft..." -ForegroundColor Cyan
    
    $jobName = "StarCraft_Download"
    $existingJob = Get-BitsTransfer -Name $jobName -ErrorAction SilentlyContinue
    if ($existingJob) {
        Write-Host "Resuming previous download..." -ForegroundColor Yellow
        Resume-BitsTransfer -BitsJob $existingJob -Asynchronous
        $job = $existingJob
    } else {
        $job = Start-BitsTransfer -Source $Url -Destination $ZipPath -DisplayName $jobName -Description "Downloading StarCraft" -Asynchronous
    }
    
    while ($job.JobState -eq "Transferring" -or $job.JobState -eq "Connecting") {
        $progress = [int](($job.BytesTransferred / $job.BytesTotal) * 100)
        Write-Progress -Activity "Downloading StarCraft" -Status "$progress% Complete" -PercentComplete $progress
        Start-Sleep -Milliseconds 500
        $job = Get-BitsTransfer -JobId $job.JobId
    }
    
    if ($job.JobState -eq "Transferred") {
        Complete-BitsTransfer -BitsJob $job
        Write-Progress -Activity "Downloading StarCraft" -Completed
    } else {
        throw "Download failed with state: $($job.JobState)"
    }
    
    Write-Host "Extracting..." -ForegroundColor Cyan
    if (Test-Path $Destination) { Remove-Item $Destination -Recurse -Force }
    Expand-Archive -Path $ZipPath -DestinationPath (Split-Path $Destination -Parent) -Force
    Remove-Item $ZipPath -Force
}

function Configure-Registry {
    param(
        [string]$InstallPath
    )
    
    New-Item -Path "HKCU:\Software\Chaoslauncher\Launcher" -Force | Out-Null
    Set-ItemProperty "HKCU:\Software\Chaoslauncher\Launcher" -Name "GameVersion" -Value "Starcraft 1.16.1"
    Set-ItemProperty "HKCU:\Software\Chaoslauncher\Launcher" -Name "RunScOnStartup" -Value 1 -Type DWord
    
    New-Item -Path "HKCU:\Software\Chaoslauncher\PluginsEnabled" -Force | Out-Null
    Set-ItemProperty "HKCU:\Software\Chaoslauncher\PluginsEnabled" -Name "BWAPI 4.4.0 Injector [RELEASE]" -Value 1 -Type DWord
    Set-ItemProperty "HKCU:\Software\Chaoslauncher\PluginsEnabled" -Name "W-MODE 1.02" -Value 1 -Type DWord
    
    $resolvedPath = (Resolve-Path $InstallPath).Path
    
    Write-Host "`n`nPress Enter to open Chaoslauncher, then go to settings and put in this path: " -ForegroundColor Yellow -NoNewline
    Write-Host $resolvedPath -ForegroundColor Green
    
    Write-Host "also, turn off the 'warn about missing admin permissions' setting: " -ForegroundColor Yellow

    Read-Host
    & "$PSScriptRoot\Starcraft\BWAPI\Chaoslauncher\Chaoslauncher.exe"
}

$url = "https://snow0-my.sharepoint.com/:u:/g/personal/alex_mickelson_snow_edu1/IQDss8Kj45-XRrhHa0wSm9OdAcdmTZTAzXHzVoUBATpH4nM?e=nPYQfx&download=1"
$zip = "$env:TEMP\Starcraft.zip"
$dest = "$PSScriptRoot\Starcraft"

if (Test-Path $dest) {
    Write-Host "StarCraft folder already exists. Skipping download..." -ForegroundColor Yellow
} else {
    Download-StarCraft -Url $url -ZipPath $zip -Destination $dest
}
Configure-Registry -InstallPath $dest
