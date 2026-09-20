# Machine-level setup is restricted to disposable GitHub-hosted runners.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Only disposable GitHub-hosted runners may use this setup script. Install WinAppDriver and enable Developer Mode manually on a personal machine.'
}
$msi = Join-Path $env:RUNNER_TEMP 'WindowsApplicationDriver_1.2.1.msi'
Invoke-WebRequest 'https://github.com/microsoft/WinAppDriver/releases/download/v1.2.1/WindowsApplicationDriver_1.2.1.msi' -OutFile $msi
if ((Get-FileHash $msi -Algorithm SHA256).Hash -ne 'A76A8F4E44B29BAD331ACF6B6C248FCC65324F502F28826AD2ACD5F3C80857FE') { throw 'WinAppDriver MSI hash mismatch.' }
if ((Get-AuthenticodeSignature $msi).Status -ne 'Valid') { throw 'WinAppDriver MSI signature is invalid.' }
$install = Start-Process msiexec.exe -ArgumentList @('/i', ('"' + $msi + '"'), '/quiet', '/norestart') -Wait -PassThru -WindowStyle Hidden
if ($install.ExitCode -notin 0,3010) { throw "WinAppDriver installation failed: $($install.ExitCode)" }
$key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
New-Item -Path $key -Force | Out-Null
New-ItemProperty -Path $key -Name AllowDevelopmentWithoutDevLicense -PropertyType DWord -Value 1 -Force | Out-Null
