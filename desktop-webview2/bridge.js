'use strict';
(() => {
  const listeners = new Set();
  window.chrome.webview.addEventListener('message', event => {
    if (event.data.kind === 'state') for (const listener of listeners) listener(event.data.state);
  });
  window.desktop = Object.freeze({
    stop: () => window.chrome.webview.postMessage({kind:'stop'}),
    ready: () => window.chrome.webview.postMessage({kind:'ready'}),
    onState: listener => {listeners.add(listener);return () => listeners.delete(listener);}
  });
})();
