param([string]$OutputDirectory=(Join-Path $PSScriptRoot 'release/PetbirdsLite-win-x64'))
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -Test
$build=Join-Path $PSScriptRoot 'bin/Release/net48'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach($name in @('PetbirdsLite.exe','PetbirdsLite.exe.config','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Wpf.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','robin.ico','robin.png')) {
    Copy-Item -LiteralPath (Join-Path $build $name) -Destination (Join-Path $OutputDirectory $name)
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'web') -Destination $OutputDirectory -Recurse -Force
$instructions=@'
Petbirds 轻量桌面版 v2.2.0（Windows x64）

1. 完整解压后运行 PetbirdsLite.exe。
2. 先从托盘退出之前运行的版本。新版启动时只显示一个悬浮知更鸟。
3. 左键点击知更鸟展开/收起五个圆形动作按钮，按住知更鸟拖动可移动位置。
4. 回到桌面，选择动作后点击壁纸显示手势、爱心、白色“啥”或㊗️；选择后自动收起菜单，壁纸保持静止。
5. 互动期间会覆盖所在屏幕的工作区以接收点击。鼠标右键或按 Esc 结束互动，撤掉覆盖层。
6. 右键知更鸟选择“隐藏”或“退出程序”。隐藏会同时结束互动；点击右下角托盘小鸟可重新显示。
7. 知更鸟使用普通透明置顶窗口，会显示在其他应用上方；空闲时只占小图标区域。不再挂载 Windows 桌面层。

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
