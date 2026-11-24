# Petbirds

上传喜欢的小动物照片，选择戳一戳、亲亲、啥子、摸摸或祝福，与照片互动。

- [打开网页版](https://banirabbit.github.io/petbirds/)
- [下载 Windows 桌面版](https://github.com/banirabbit/petbirds/releases/latest)

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

## Windows 桌面版

桌面版使用 Electron。解压便携版压缩包后，运行 `PetbirdsDesktop.exe`，保留同目录的全部文件。

- 先回到 Windows 桌面，再点击悬浮工具条中的动作。桌面版不需要上传图片，直接在现有壁纸上显示特效。
- 戳一戳、亲亲、啥子、摸摸和祝福均可使用，壁纸本身不会摇晃或缩放。
- 点击 `×` 结束互动，立即清空特效并恢复正常桌面操作；也可以按 `Esc` 或点击鼠标右键结束。
- 切换到其他应用时会自动结束互动。空闲时互动层隐藏，桌面图标可以正常点击。
- 拖动工具条顶部的小横条可移动位置；底部按钮可折叠工具条，结束按钮始终保留。
- 右键工具条或托盘图标，选择“退出 Petbirds”可彻底退出。程序不设置开机启动。

从源码运行需要 Windows、Node.js 22.12 或更新版本及 npm：

```powershell
cd desktop
npm ci
npm start
```

执行 `npm test` 验证互动状态，执行 `npm run package` 生成 `desktop/release/PetbirdsDesktop-win32-x64` 便携程序目录。首次启动或打包需要下载 Electron 官方运行时。`shared/effects.js` 由网页与桌面版共用。
