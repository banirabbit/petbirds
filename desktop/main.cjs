'use strict';
const {app, BrowserWindow, ipcMain, screen, Tray, Menu, nativeImage, globalShortcut, dialog} = require('electron');
const path = require('node:path');
const fs = require('node:fs');
const {spawn} = require('node:child_process');
const {InteractionController} = require('./controller.cjs');

let toolbar, overlay, tray, watcher, watchdog, controller, quitting = false;
let lastHeartbeat = 0;
if (!app.requestSingleInstanceLock()) app.quit();
else {
  app.on('second-instance', () => { toolbar?.showInactive(); toolbar?.moveTop(); });
  app.whenReady().then(boot).catch(error => {
    dialog.showErrorBox('Petbirds 无法启动', error.message);
    app.quit();
  });
}

function send(win, state) {
  if (win && !win.isDestroyed() && !win.webContents.isDestroyed()) win.webContents.send('interaction:state',state);
}
function lockedWindow(options) {
  const win = new BrowserWindow({
    show:false, frame:false, transparent:true, backgroundColor:'#00000000',
    resizable:false, minimizable:false, maximizable:false, fullscreenable:false,
    skipTaskbar:true, focusable:false, hasShadow:false, alwaysOnTop:true,
    webPreferences:{preload:path.join(__dirname,'preload.cjs'),nodeIntegration:false,contextIsolation:true,sandbox:true},
    ...options
  });
  win.webContents.setWindowOpenHandler(() => ({action:'deny'}));
  win.webContents.on('will-navigate', event => event.preventDefault());
  win.webContents.session.setPermissionRequestHandler((_contents,_permission,callback) => callback(false));
  win.webContents.on('render-process-gone', () => { if (!quitting) app.quit(); });
  win.on('unresponsive', () => controller?.stop());
  return win;
}
async function boot() {
  if (process.platform !== 'win32') throw new Error('此版本适用于 Windows。');
  const area = screen.getPrimaryDisplay().workArea;
  toolbar = lockedWindow({title:'Petbirds 工具条',width:88,height:472,x:area.x+area.width-112,y:area.y+Math.max(0,Math.round((area.height-472)/2))});
  overlay = lockedWindow({title:'Petbirds 互动层',...area});
  overlay.setIgnoreMouseEvents(true);
  controller = new InteractionController({
    show() {
      const display = screen.getDisplayMatching(toolbar.getBounds());
      overlay.setBounds(display.workArea);
      overlay.setIgnoreMouseEvents(false);
      overlay.showInactive();
      toolbar.moveTop();
      if (!globalShortcut.isRegistered('Escape')) globalShortcut.register('Escape', () => controller.stop());
    },
    hide() {
      if(overlay && !overlay.isDestroyed()) {
        overlay.setIgnoreMouseEvents(true);
        overlay.hide();
      }
      globalShortcut.unregister('Escape');
    },
    broadcast(state) { send(toolbar,state); send(overlay,state); }
  });
  ipcMain.on('interaction:select',(event,mode) => {
    if (event.sender === toolbar.webContents && typeof mode === 'string') controller.start(mode);
  });
  ipcMain.on('interaction:stop',event => {
    if ([toolbar.webContents,overlay.webContents].includes(event.sender)) controller.stop();
  });
  ipcMain.on('toolbar:resize',(event,height) => {
    if (event.sender !== toolbar.webContents || !Number.isFinite(height)) return;
    const current = toolbar.getBounds();
    const area = screen.getDisplayMatching(current).workArea;
    const nextHeight = Math.min(area.height,Math.max(120,Math.ceil(height)));
    toolbar.setBounds({x:Math.max(area.x,Math.min(current.x,area.x+area.width-current.width)),y:Math.max(area.y,Math.min(current.y,area.y+area.height-nextHeight)),width:current.width,height:nextHeight});
  });
  ipcMain.on('toolbar:menu',event => {
    if(event.sender===toolbar.webContents) contextMenu().popup({window:toolbar});
  });
  ipcMain.handle('interaction:get',event => {
    if([toolbar.webContents,overlay.webContents].includes(event.sender)) return controller.snapshot();
    return null;
  });
  await Promise.all([toolbar.loadFile(path.join(__dirname,'ui/toolbar.html')),overlay.loadFile(path.join(__dirname,'ui/overlay.html'))]);
  toolbar.showInactive();
  tray = new Tray(makeTrayIcon());
  tray.setToolTip('Petbirds 桌面互动');
  tray.setContextMenu(contextMenu());
  tray.on('click',() => {toolbar.showInactive();toolbar.moveTop();});
  toolbar.on('close',() => {if(!quitting) app.quit();});
  screen.on('display-removed',() => {controller.stop();resetToolbar();});
  screen.on('display-metrics-changed',() => {controller.stop();resetToolbar();});
  startDesktopWatch();
}
function contextMenu() {
  return Menu.buildFromTemplate([
    {label:'显示工具条',click:() => {toolbar.showInactive();toolbar.moveTop();}},
    {label:'结束互动',click:() => controller.stop()},
    {type:'separator'},
    {label:'退出 Petbirds',click:() => app.quit()}
  ]);
}
function resetToolbar() {
  const area=screen.getPrimaryDisplay().workArea;
  const bounds=toolbar.getBounds();
  toolbar.setPosition(area.x+area.width-bounds.width-24,area.y+Math.max(0,Math.round((area.height-bounds.height)/2)));
}
function startDesktopWatch() {
  const executable=path.join(process.env.SystemRoot || 'C:\\Windows','System32/WindowsPowerShell/v1.0/powershell.exe');
  const script=fs.readFileSync(path.join(__dirname,'native/desktop-watch.ps1'),'utf8');
  watcher=spawn(executable,['-NoProfile','-NonInteractive','-Command',script],{windowsHide:true,stdio:['ignore','pipe','pipe']});
  let buffer='';
  watcher.stdout.setEncoding('utf8');
  watcher.stdout.on('data',chunk => {
    buffer+=chunk;
    const lines=buffer.split(/\r?\n/); buffer=lines.pop();
    for(const line of lines) {
      if(line!=='desktop' && line!=='other') continue;
      const wasReady=controller.ready;
      lastHeartbeat=Date.now();
      if(!wasReady || controller.desktop!==(line==='desktop')) controller.updateDesktop(line==='desktop');
    }
  });
  watcher.stderr.on('data',chunk => {console.error('Desktop monitor:',chunk.toString().trim());});
  watcher.on('error',() => controller.unavailable());
  watcher.on('exit',() => {if(!quitting) controller.unavailable();});
  const began=Date.now();
  let reportedFailure=false;
  watchdog=setInterval(() => {
    if((lastHeartbeat && Date.now()-lastHeartbeat>2500) || (!lastHeartbeat && Date.now()-began>15000)) {
      if(!reportedFailure) {controller.unavailable();reportedFailure=true;}
    } else reportedFailure=false;
  },1000);
}
function makeTrayIcon() {
  const pixels=Buffer.alloc(32*32*4);
  for(let y=0;y<32;y++) for(let x=0;x<32;x++) {
    const dx=(x-15.5)/13,dy=(15-y)/13;
    if(Math.pow(dx*dx+dy*dy-1,3)-dx*dx*dy*dy*dy<=0) {
      const i=(y*32+x)*4; pixels[i]=148;pixels[i+1]=101;pixels[i+2]=238;pixels[i+3]=255;
    }
  }
  return nativeImage.createFromBitmap(pixels,{width:32,height:32,scaleFactor:1});
}
app.on('before-quit',() => {
  quitting=true;
  controller?.stop();
  clearInterval(watchdog);
  watcher?.kill();
  globalShortcut.unregisterAll();
  tray?.destroy();
});
app.on('window-all-closed',() => app.quit());
