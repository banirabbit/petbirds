const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');

test('WebView2 bridge carries actions, stop, resize, dragging and state replies',async()=>{
 const sent=[];
 let receive;
 const window={chrome:{webview:{postMessage:message=>sent.push(message),addEventListener:(_name,listener)=>{receive=listener;}}}};
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../bridge.js'),'utf8'),{window,setTimeout,clearTimeout});
 const api=window.desktop;
 assert.ok(Object.isFrozen(api));
 api.select('pet');api.stop();api.resize(472);api.drag();api.menu();
 assert.equal(JSON.stringify(sent),JSON.stringify([{kind:'select',mode:'pet'},{kind:'stop'},{kind:'resize',height:472},{kind:'drag'},{kind:'menu'}]));
 let latest;
 const unsubscribe=api.onState(state=>{latest=state;});
 receive({data:{kind:'state',state:{mode:'shazi'}}});assert.equal(latest.mode,'shazi');
 unsubscribe();receive({data:{kind:'state',state:{mode:null}}});assert.equal(latest.mode,'shazi');
 const pending=api.state();
 const request=sent.at(-1);
 receive({data:{kind:'stateReply',id:request.id,state:{ready:true,mode:null}}});
 assert.deepEqual(await pending,{ready:true,mode:null});
 api.ready();assert.equal(sent.at(-1).kind,'ready');
});
