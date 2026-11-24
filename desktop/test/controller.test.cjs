const {test}=require('node:test');
const assert=require('node:assert/strict');
const {InteractionController}=require('../controller.cjs');
function setup(){
 const log=[];
 const controller=new InteractionController({show:()=>log.push('show'),hide:()=>log.push('hide'),broadcast:state=>log.push(state)});
 return {controller,log};
}
test('interaction starts only on the desktop and rejects unknown modes',()=>{
 const {controller,log}=setup();
 controller.start('kiss');assert.equal(controller.mode,null);assert.ok(!log.includes('show'));
 controller.updateDesktop(false);controller.start('pet');assert.equal(controller.mode,null);
 controller.updateDesktop(true);controller.start('bless');assert.equal(controller.mode,'bless');assert.ok(log.includes('show'));
 controller.start('unexpected');assert.equal(controller.mode,'bless');
});
test('end button and switching to another app remove the input overlay',()=>{
 const {controller,log}=setup();
 controller.updateDesktop(true);controller.start('kiss');controller.stop();
 assert.equal(controller.mode,null);assert.equal(log.at(-2),'hide');assert.equal(log.at(-1).mode,null);
 controller.start('shazi');controller.updateDesktop(false);
 assert.equal(controller.mode,null);assert.equal(log.at(-2),'hide');
});
test('monitor failure ends interaction and prevents starting until recovery',()=>{
 const {controller,log}=setup();
 controller.updateDesktop(true);controller.start('poke');controller.unavailable();
 assert.equal(controller.ready,false);assert.equal(controller.mode,null);assert.equal(log.at(-2),'hide');
 controller.start('pet');assert.equal(controller.mode,null);
 controller.updateDesktop(true);controller.start('pet');assert.equal(controller.mode,'pet');
});
