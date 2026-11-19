param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '.NET Framework 4.x compiler (csc.exe) not found' }

$dist = Join-Path $root 'dist'
$obj = Join-Path $root 'obj'
New-Item -ItemType Directory -Force $dist, $obj | Out-Null

$ico = Join-Path $root 'assets\icon.ico'
if (-not (Test-Path $ico)) {
    & $csc /nologo /codepage:65001 /optimize+ "/out:$obj\MakeIcon.exe" /r:System.Drawing.dll "$root\tools\MakeIcon.cs"
    if ($LASTEXITCODE) { throw 'MakeIcon build failed' }
    & "$obj\MakeIcon.exe" $ico "$root\assets\icon-preview.png"
    if ($LASTEXITCODE) { throw 'MakeIcon failed' }
}

$src = Get-ChildItem "$root\src\*.cs" | ForEach-Object { $_.FullName }
& $csc /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu "/out:$dist\SleepTimer.exe" "/win32icon:$ico" "/win32manifest:$root\src\app.manifest" "/resource:$ico,SleepTimer.icon.ico" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $src
if ($LASTEXITCODE) { throw 'Build failed' }
$size = [math]::Round((Get-Item "$dist\SleepTimer.exe").Length / 1KB)
Write-Host "OK dist\SleepTimer.exe ($size KB)"

if ($Test) {
    $t = Join-Path $obj 'test'
    New-Item -ItemType Directory -Force $t | Out-Null
    Copy-Item "$dist\SleepTimer.exe" $t -Force
    & $csc /nologo /codepage:65001 /target:exe "/out:$t\SleepTimerTests.exe" "/r:$t\SleepTimer.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll "$root\tests\Tests.cs"
    if ($LASTEXITCODE) { throw 'Test build failed' }
    & "$t\SleepTimerTests.exe" $t
    if ($LASTEXITCODE) { throw "Tests failed: $LASTEXITCODE" }
}
