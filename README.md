# Petbirds

上传喜欢的小动物照片，选择戳一戳、亲亲、啥子、摸摸或祝福，与照片互动。

- [打开网页版](https://banirabbit.github.io/petbirds/)
- [下载 Windows 轻量版](https://github.com/banirabbit/petbirds/releases/tag/desktop-webview2-v2.1.0-preview.1)

## 轻量桌面版（系统 WebView2）

[下载轻量版预览包](https://github.com/banirabbit/petbirds/releases/tag/desktop-webview2-v2.1.0-preview.1)。完整解压后运行 `PetbirdsLite.exe`。

`desktop-webview2/` 是 C# / WPF + 系统 WebView2 的轻量版本，不随应用分发 Chromium 或 Node.js。程序和托盘使用像素知更鸟图标。

运行要求：Windows 10/11 x64、.NET Framework 4.8、Microsoft Edge WebView2 Evergreen Runtime。缺失 WebView2 时程序会提示；[微软官方下载](https://developer.microsoft.com/microsoft-edge/webview2/)。

完整解压轻量版运行包后运行 `PetbirdsLite.exe`。先从托盘退出之前运行的版本，再启动新版。工具条与特效窗口作为 Windows 桌面图标宿主的子窗口运行，应用窗口自然盖在它们上方；露出的桌面仍可互动。不根据前台应用反复隐藏/显示工具条，也不全局置顶。鼠标右键或 `Esc` 结束互动，工具条没有独立结束按钮。五种动作、拖动/折叠和托盘退出均保留，壁纸保持静止。浏览器缓存位于 `%LOCALAPPDATA%\PetbirdsLite\WebView2`。

窗口挂到承载 `SHELLDLL_DefView` 的桌面宿主（Progman/WorkerW），只调整同一桌面宿主内部的子窗口顺序。多显示器定位将屏幕坐标转换为桌面宿主坐标。Windows Explorer 重启或桌面宿主被重建后，需要重新打开程序。

从源码构建需要 Windows 和 .NET SDK（已验证 9.0.305）：

```powershell
./desktop-webview2/Build.ps1 -Test
./desktop-webview2/Package.ps1
```

运行文件生成于 `desktop-webview2/release/PetbirdsLite-win-x64/`。构建脚本从 `desktop/ui/` 与 `shared/` 组装前端资源；客户端不需要 Node.js 或 PowerShell 后台进程。`assets/robin-source.png` 是确认的原始图标，`Build-Icons.ps1` 只进行尺寸转换并生成多尺寸 ICO。

## 使用

下载仓库后，直接用浏览器打开 `index.html`，无需安装依赖或启动服务。

- 初始显示“待上传”，点击中央区域或图片按钮选择图片。
- 电脑端使用右侧圆形按钮切换动作，鼠标进入图片后显示对应光标。
- 手机端图片等比例完整显示并居中适应屏幕，点击右下角悬浮按钮展开或收起工具。
- 亲亲会冒爱心；戳一戳和摸摸各有互动动画。
- 选择“啥子”后，点击图片会在正中央弹出一个巨大的白色“啥”。
- 选择㊗️祝福后，点击图片会在正中央弹出一个巨大的㊗️。
- 图片只在当前浏览器页面中读取，不会上传到服务器；刷新页面后需重新选择。

## 文件

- `index.html`：页面入口。
- `style.css`：桌面和移动端样式。
- `app.js`：图片读取、工具菜单、光标和互动动画。
- `shared/effects.js`：网页与桌面版共用的图标和特效。
- `desktop/`：Windows 桌面版源码、构建配置与测试。

使用原生 HTML、CSS 和 JavaScript，无第三方依赖。

## Electron 桌面版（旧版）

桌面版使用 Electron。解压便携版压缩包后，运行 `PetbirdsDesktop.exe`，保留同目录的全部文件。

- 先回到 Windows 桌面，再点击悬浮工具条中的动作。桌面版不需要上传图片，直接在现有壁纸上显示特效。
- 戳一戳、亲亲、啥子、摸摸和祝福均可使用，壁纸本身不会摇晃或缩放。
- 点击鼠标右键或按 `Esc` 结束互动，立即清空特效并恢复正常桌面操作。
- 切换到其他应用时会自动结束互动。空闲时互动层隐藏，桌面图标可以正常点击。
- 拖动工具条顶部的小横条可移动位置；底部按钮可折叠工具条。
- 右键工具条或托盘图标，选择“退出 Petbirds”可彻底退出。程序不设置开机启动。

从源码运行需要 Windows、Node.js 22.12 或更新版本及 npm：

```powershell
cd desktop
npm ci
npm start
```

执行 `npm test` 验证互动状态，执行 `npm run package` 生成 `desktop/release/PetbirdsDesktop-win32-x64` 便携程序目录。首次启动或打包需要下载 Electron 官方运行时。`shared/effects.js` 由网页与桌面版共用。
