$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$hostExe = (Resolve-Path '..\dist\Hryushki_Accounts.exe').Path
$work = Join-Path $env:RUNNER_TEMP 'HryushkiBusinessPluginTests'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item $hostExe (Join-Path $work 'Hryushki_Accounts.exe')
New-Item -ItemType Directory -Path release -Force | Out-Null
foreach ($name in @('Profitability','StockPlanner')) {
  $target = Join-Path $work $name
  New-Item -ItemType Directory -Path $target -Force | Out-Null
  $dll = Join-Path $target "$name.dll"
  & $csc /nologo /codepage:65001 /target:library /optimize+ "/out:$dll" "/r:$hostExe" /r:System.Core.dll Common.cs "$name\$name.cs"
  if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $name" }
  Copy-Item "$name\*.json" $target
  Copy-Item $dll $work
  Compress-Archive -Path "$target\*" -DestinationPath "release\${name}_1.0.zip" -Force
}
Copy-Item README.txt release/
$tests = Join-Path $work 'PluginTests.exe'
& $csc /nologo /codepage:65001 /target:exe "/out:$tests" "/r:$hostExe" "/r:$work\Profitability.dll" "/r:$work\StockPlanner.dll" /r:System.Core.dll PluginTests.cs
if ($LASTEXITCODE -ne 0) { throw 'Tests compilation failed' }
$profit = (Resolve-Path release/Profitability_1.0.zip).Path
$stock = (Resolve-Path release/StockPlanner_1.0.zip).Path
$p = Start-Process $tests -ArgumentList @('--lifecycle',"`"$profit`"","`"$stock`"") -Wait -PassThru -NoNewWindow -RedirectStandardOutput plugin-tests.log -RedirectStandardError plugin-tests.err
Get-Content plugin-tests.log
Get-Content plugin-tests.err
if ($p.ExitCode -ne 0) { throw 'Business plugins tests failed' }
