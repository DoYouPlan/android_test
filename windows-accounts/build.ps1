$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
New-Item -ItemType Directory -Path dist -Force | Out-Null
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /win32manifest:app.manifest /out:dist/Hryushki_Accounts.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Core.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll Domain.cs Plugins.cs Ui.cs Program.cs
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed' }
New-Item -ItemType Directory -Path dist/PluginSDK -Force | Out-Null
& $csc /nologo /target:library /out:dist/PluginSDK/ExampleReport.dll /r:dist/Hryushki_Accounts.exe /r:System.Core.dll PluginSDK/ExampleReport.cs
if ($LASTEXITCODE -ne 0) { throw 'Plugin compilation failed' }
Copy-Item PluginSDK/plugin.json dist/PluginSDK/
Copy-Item PluginSDK/ExampleReport.cs dist/PluginSDK/
Copy-Item PluginSDK/README.md dist/PluginSDK/
Copy-Item README.txt dist/
Compress-Archive -Path dist/PluginSDK/ExampleReport.dll,dist/PluginSDK/plugin.json -DestinationPath dist/PluginSDK/ExampleReport.zip -Force
