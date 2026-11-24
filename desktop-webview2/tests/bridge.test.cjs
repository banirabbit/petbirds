const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');

test('overlay bridge delivers state and returns right-click stop to the host',()=>{
 const sent=[];
 let receive;
 const window={chrome:{webview:{postMessage:message=>sent.push(message),addEventListener:(_name,listener)=>{receive=listener;}}}};
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../bridge.js'),'utf8'),{window});
 const api=window.desktop;
 let mode;
 const unsubscribe=api.onState(state=>{mode=state.mode;});
 receive({data:{kind:'state',state:{mode:'pet'}}});
 assert.equal(mode,'pet');
 unsubscribe();
 receive({data:{kind:'state',state:{mode:null}}});
 assert.equal(mode,'pet');
 api.ready();api.stop();
 assert.equal(JSON.stringify(sent),JSON.stringify([{kind:'ready'},{kind:'stop'}]));
});
