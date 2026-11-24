'use strict';
const {contextBridge,ipcRenderer}=require('electron');
contextBridge.exposeInMainWorld('desktop',{
  select:mode=>ipcRenderer.send('interaction:select',mode),
  stop:()=>ipcRenderer.send('interaction:stop'),
  resize:height=>ipcRenderer.send('toolbar:resize',height),
  menu:()=>ipcRenderer.send('toolbar:menu'),
  state:()=>ipcRenderer.invoke('interaction:get'),
  onState:callback=>{
    const listener=(_event,state)=>callback(state);
    ipcRenderer.on('interaction:state',listener);
    return ()=>ipcRenderer.removeListener('interaction:state',listener);
  }
});
