'use strict';
const cursor=document.querySelector('#cursor');
const effects=PetbirdsEffects.createEffects(document.querySelector('#effects'),matchMedia('(prefers-reduced-motion:reduce)'));
let mode=null,down=false,lastStroke=0;
window.desktop.onState(state=>{
 mode=state.mode;down=false;effects.clear();cursor.classList.remove('visible');
 cursor.innerHTML=mode?PetbirdsEffects.svg(mode):'';
});
function play(event){
 if(!mode)return;
 effects.play(mode,event.clientX,event.clientY,{left:0,top:0,width:innerWidth,height:innerHeight});
}
document.addEventListener('pointerdown',event=>{
 if(!event.isPrimary || event.button!==0)return;
 down=true;lastStroke=performance.now();play(event);
});
document.addEventListener('pointermove',event=>{
 cursor.style.transform=`translate(${event.clientX-20}px,${event.clientY-20}px)`;
 cursor.classList.toggle('visible',!!mode);
 if(down && mode==='pet' && performance.now()-lastStroke>=120){lastStroke=performance.now();play(event);}
});
['pointerup','pointercancel','pointerleave'].forEach(name=>document.addEventListener(name,()=>{down=false;if(name==='pointerleave')cursor.classList.remove('visible');}));
document.addEventListener('contextmenu',event=>{event.preventDefault();window.desktop.stop();});
