'use strict';
(() => {
  const listeners = new Set();
  const requests = new Map();
  let sequence = 0;
  const send = message => window.chrome.webview.postMessage(message);
  window.chrome.webview.addEventListener('message', event => {
    const message = event.data;
    if (message.kind === 'state') for (const listener of listeners) listener(message.state);
    if (message.kind === 'stateReply' && requests.has(message.id)) {
      const request = requests.get(message.id);
      clearTimeout(request.timer);
      requests.delete(message.id);
      request.resolve(message.state);
    }
  });
  window.desktop = Object.freeze({
    select: mode => send({kind:'select',mode}),
    stop: () => send({kind:'stop'}),
    resize: height => send({kind:'resize',height}),
    drag: () => send({kind:'drag'}),
    menu: () => send({kind:'menu'}),
    ready: () => send({kind:'ready'}),
    state: () => new Promise((resolve,reject) => {
      const id = ++sequence;
      const timer = setTimeout(() => {requests.delete(id);reject(new Error('桌面状态读取超时'));},10000);
      requests.set(id,{resolve,timer});
      send({kind:'state',id});
    }),
    onState: listener => {listeners.add(listener);return () => listeners.delete(listener);}
  });
})();
