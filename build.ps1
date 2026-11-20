param([switch]$Test, [switch]$Shortcut)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '.NET Framework 4.x compiler (csc.exe) not found' }

$obj = Join-Path $root 'obj'
$exe = Join-Path $root 'SleepTimer.exe'
New-Item -ItemType Directory -Force $obj | Out-Null

$ico = Join-Path $root 'assets\icon.ico'
if (-not (Test-Path $ico)) {
    & $csc /nologo /codepage:65001 /optimize+ "/out:$obj\MakeIcon.exe" /r:System.Drawing.dll "$root\tools\MakeIcon.cs"
    if ($LASTEXITCODE) { throw 'MakeIcon build failed' }
    & "$obj\MakeIcon.exe" $ico "$root\assets\icon.png"
    if ($LASTEXITCODE) { throw 'MakeIcon failed' }
}

$src = Get-ChildItem "$root\src\*.cs" | ForEach-Object { $_.FullName }
& $csc /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu "/out:$obj\SleepTimer.exe" "/win32icon:$ico" "/win32manifest:$root\src\app.manifest" "/resource:$ico,SleepTimer.icon.ico" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $src
if ($LASTEXITCODE) { throw 'Build failed' }
try { Copy-Item "$obj\SleepTimer.exe" $exe -Force }
catch { throw 'SleepTimer.exe is running. Close it (tray icon > Exit) and build again.' }
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower()
"$hash  SleepTimer.exe" | Set-Content (Join-Path $obj 'SHA256SUMS.txt') -Encoding ascii
$size = [math]::Round((Get-Item $exe).Length / 1KB)
Write-Host "OK SleepTimer.exe ($size KB)"

if ($Shortcut) {
    $lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'SleepTimer.lnk'
    $ws = New-Object -ComObject WScript.Shell
    $s = $ws.CreateShortcut($lnk)
    $s.TargetPath = $exe
    $s.WorkingDirectory = $root
    $s.IconLocation = "$exe,0"
    $s.Description = 'SleepTimer'
    $s.Save()
    Write-Host "OK desktop shortcut: $lnk"
}

if ($Test) {
    $t = Join-Path $obj 'test'
    New-Item -ItemType Directory -Force $t | Out-Null
    Copy-Item $exe $t -Force
    & $csc /nologo /codepage:65001 /target:exe "/out:$t\SleepTimerTests.exe" "/r:$t\SleepTimer.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll "$root\tests\Tests.cs"
    if ($LASTEXITCODE) { throw 'Test build failed' }
    & "$t\SleepTimerTests.exe" $t
    if ($LASTEXITCODE) { throw "Tests failed: $LASTEXITCODE" }
}
