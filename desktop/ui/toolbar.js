'use strict';
const {actions,svg}=PetbirdsEffects;
const actionsNode=document.querySelector('#actions');
const stop=document.querySelector('#stop');
const collapse=document.querySelector('#collapse');
const notice=document.querySelector('#notice');
let timer;
for(const action of actions) {
 const button=document.createElement('button');
 button.className='round';button.type='button';button.title=action.label;
 button.setAttribute('aria-label',action.label);button.setAttribute('aria-pressed','false');
 button.dataset.mode=action.id;button.innerHTML=svg(action.id);
 button.addEventListener('click',()=>window.desktop.select(action.id));
 actionsNode.append(button);
}
stop.innerHTML=svg('close');stop.addEventListener('click',()=>window.desktop.stop());
function fit(){window.desktop.resize(document.querySelector('#toolbar').getBoundingClientRect().height+5);}
function updateCollapse(){
 const closed=actionsNode.hidden;
 collapse.innerHTML=svg(closed?'expand':'collapse');
 collapse.title=closed?'展开工具条':'收起工具条';
 collapse.setAttribute('aria-label',collapse.title);collapse.setAttribute('aria-expanded',String(!closed));
 fit();
}
collapse.addEventListener('click',()=>{actionsNode.hidden=!actionsNode.hidden;updateCollapse();});
document.addEventListener('contextmenu',event=>{event.preventDefault();window.desktop.menu();});
function render(state){
 if(!state)return;
 actionsNode.querySelectorAll('button').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.mode===state.mode)));
 stop.disabled=!state.mode;
 if(state.message){notice.textContent=state.message;notice.classList.add('visible');clearTimeout(timer);timer=setTimeout(()=>notice.classList.remove('visible'),2600);}
}
window.desktop.onState(render);window.desktop.state().then(render);
updateCollapse();
