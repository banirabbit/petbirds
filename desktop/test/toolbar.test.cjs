const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');

test('toolbar has five actions without an end button; right-click stops active mode',async()=>{
 class Node {
  constructor(){this.children=[];this.dataset={};this.listeners={};this.hidden=false;this.attributes={};this.classList={add(){},remove(){}};}
  setAttribute(name,value){this.attributes[name]=value;}
  addEventListener(name,callback){this.listeners[name]=callback;}
  append(child){this.children.push(child);}
  querySelectorAll(){return this.children;}
  getBoundingClientRect(){return {height:403};}
 }
 const nodes=Object.fromEntries(['#actions','#collapse','#notice','#toolbar','.grip'].map(name=>[name,new Node()]));
 const events={};
 let selected,stops=0,menus=0,render;
 const document={querySelector:name=>{assert.ok(nodes[name],`unexpected node ${name}`);return nodes[name];},createElement:()=>new Node(),addEventListener:(name,callback)=>{events[name]=callback;}};
 const window={desktop:{select:mode=>{selected=mode;},stop:()=>{stops++;render({mode:null});},menu:()=>{menus++;},resize(){},drag(){},state:()=>Promise.resolve({mode:null}),onState:callback=>{render=callback;}}};
 const context={document,window,setTimeout:()=>1,clearTimeout(){}};
 const root=path.resolve(__dirname,'../..');
 vm.runInNewContext(fs.readFileSync(path.join(root,'shared/effects.js'),'utf8'),context);
 vm.runInNewContext(fs.readFileSync(path.join(root,'desktop/ui/toolbar.js'),'utf8'),context);
 await Promise.resolve();
 assert.equal(nodes['#actions'].children.length,5);
 assert.ok(!fs.readFileSync(path.join(root,'desktop/ui/toolbar.html'),'utf8').includes('id="stop"'));
 nodes['#actions'].children[0].listeners.click();assert.equal(selected,'poke');
 render({mode:'pet'});
 events.contextmenu({preventDefault(){}});assert.equal(stops,1);assert.equal(menus,0);
 events.contextmenu({preventDefault(){}});assert.equal(menus,1,'idle right-click opens the application menu');
 nodes['#collapse'].listeners.click();assert.equal(nodes['#actions'].hidden,true);
});
