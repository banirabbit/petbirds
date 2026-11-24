param([string]$OutputDirectory=(Join-Path $PSScriptRoot 'release/PetbirdsLite-win-x64'))
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -Test
$build=Join-Path $PSScriptRoot 'bin/Release/net48'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach($name in @('PetbirdsLite.exe','PetbirdsLite.exe.config','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Wpf.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','robin.ico')) {
    Copy-Item -LiteralPath (Join-Path $build $name) -Destination (Join-Path $OutputDirectory $name)
}
Copy-Item -LiteralPath (Join-Path $build 'web') -Destination $OutputDirectory -Recurse -Force
$instructions=@'
Petbirds 轻量桌面版 v2.0.0（Windows x64）

1. 完整解压后运行 PetbirdsLite.exe。
2. 先退出旧 Electron 版，再回到桌面，选择工具条中的动作。
3. 点击壁纸显示手势、爱心、白色“啥”或㊗️；壁纸本身保持静止。
4. 点击 ×、按 Esc、鼠标右键或切换到其他应用，结束互动。
5. 拖动顶部横条移动工具条，底部按钮可折叠。右键工具条/托盘图标可退出。

依赖：Windows 10/11 x64、.NET Framework 4.8、Microsoft Edge WebView2 Evergreen Runtime。
运行包不包含浏览器内核。这些系统组件缺失时需要先安装。
WebView2：https://developer.microsoft.com/microsoft-edge/webview2/
.NET Framework 4.8：https://dotnet.microsoft.com/download/dotnet-framework/net48
网页版：https://banirabbit.github.io/petbirds/
源码：https://github.com/banirabbit/petbirds/tree/main/desktop-webview2

用户数据（WebView2 缓存）：%LOCALAPPDATA%\PetbirdsLite\WebView2
程序不设置开机启动，不读取或修改壁纸文件，不上传图片。
'@
[IO.File]::WriteAllText((Join-Path $OutputDirectory '使用说明.txt'),$instructions,(New-Object Text.UTF8Encoding($false)))
$packageCache=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'}
foreach($name in @('LICENSE.txt','NOTICE.txt')) {
    $license=Join-Path $packageCache ('microsoft.web.webview2/1.0.4370.26/'+$name)
    Copy-Item -LiteralPath $license -Destination (Join-Path $OutputDirectory ('WebView2-SDK-'+$name))
}
$OutputDirectory
