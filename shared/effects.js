'use strict';
globalThis.PetbirdsEffects = (() => {
  const paths = {
    upload:'<rect x="3" y="5" width="17" height="16" rx="3"/><path d="m4 17 5-5 4 4 3-3 4 4M18 3v6m-3-3h6"/><circle cx="8" cy="9" r="1"/>',
    poke:'<path d="M8 13V4.5c0-2.7 3.8-2.7 3.8 0v5c0-2.1 3-2.1 3 0v1c0-1.9 2.8-1.9 2.8 0v1c0-1.7 2.8-1.7 2.8 0v3.7c0 2.7-1.4 4.7-3.2 6H9c-1.2-2-2.3-3.6-3.6-5l-1.5-1.6c-1.8-1.9.5-4.1 2.3-2.5L8 14" fill="#fffaf6" stroke-width="1.6"/><path d="M11.8 9.5v3m3-2v2.2m2.8-1.2v1.7" stroke-width="1.3"/><ellipse cx="14.6" cy="16.7" rx="2.7" ry="1.8" fill="#f7c8d7" stroke="none"/><path d="M9 21.2h8.2" stroke="#ed9eb9" stroke-width="2.4"/>',
    kiss:'<path d="M2 12c4-2 5-8 10-4 5-4 6 2 10 4-4 3-6 7-10 7S5 15 2 12Z"/><path d="M2 12c4 0 6 2 10 1 4 1 6-1 10-1"/>',
    shazi:'<text x="12" y="12" text-anchor="middle" dominant-baseline="central" font-family="system-ui,Microsoft YaHei,sans-serif" font-size="20" font-weight="800" fill="currentColor" stroke="none">啥</text>',
    bless:'<text x="12" y="12" text-anchor="middle" dominant-baseline="central" font-family="Apple Color Emoji,Segoe UI Emoji,Noto Color Emoji,sans-serif" font-size="21" fill="currentColor" stroke="none">㊗️</text>',
    pet:'<path d="m5 20 1-7c0-2 2-2 2 0l1-5c0-2 3-2 3 0l-1 5 2-7c1-2 3-1 2 1l-1 7 2-5c1-2 3-1 2 1l-1 5c3-2 5 0 3 2l-4 4Z" fill="#ffffff" stroke="#66544c"/><path d="M3 5 1 3m7 0V1m12 3 2-2"/>',
    heart:'<path d="M12 21 3 12C-4 4 7-3 12 5c5-8 16-1 9 7Z" fill="currentColor" stroke="none"/>',
    sparkle:'<path d="m12 2 2.5 7.5L22 12l-7.5 2.5L12 22l-2.5-7.5L2 12l7.5-2.5Z" fill="currentColor" stroke="none"/>',
    paw:'<ellipse cx="12" cy="16" rx="6" ry="5" fill="currentColor" stroke="none"/><g fill="currentColor" stroke="none"><ellipse cx="4" cy="9" rx="2.4" ry="3"/><ellipse cx="9" cy="5" rx="2.3" ry="3"/><ellipse cx="15" cy="5" rx="2.3" ry="3"/><ellipse cx="20" cy="9" rx="2.4" ry="3"/></g>',
    expand:'<path d="m8 5 7 7-7 7"/>',
    collapse:'<path d="m16 5-7 7 7 7"/>',
    close:'<path d="m6 6 12 12M6 18 18 6"/>'
  };
  const svg = name => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name]}</svg>`;
  const actions = [{id:'poke',label:'戳一戳'},{id:'kiss',label:'亲亲'},{id:'shazi',label:'啥子'},{id:'pet',label:'摸摸'},{id:'bless',label:'祝福'}];

  function createEffects(effects,reduced) {
  function particle(icon, x, y, {size=24, dx=0, dy=-90, rotate=0, color='#ee6594', duration=850, delay=0}={}) {
    if (effects.childElementCount >= 100) return;
    const el = document.createElement('span');
    el.className = 'particle';
    Object.assign(el.style, {width:`${size}px`,height:`${size}px`,color});
    el.innerHTML = svg(icon);
    effects.append(el);
    const start = `translate(${x-size/2}px,${y-size/2}px)`;
    const animation = el.animate(reduced.matches ? [{transform:start,opacity:.9},{transform:start,opacity:0}] : [
      {transform:`${start} scale(.35) rotate(0deg)`,opacity:0},
      {offset:.15,transform:`${start} scale(1) rotate(${rotate/3}deg)`,opacity:1},
      {transform:`translate(${x-size/2+dx}px,${y-size/2+dy}px) scale(.8) rotate(${rotate}deg)`,opacity:0}
    ], {duration:reduced.matches?250:duration,delay:reduced.matches?0:delay,fill:'both',easing:'ease-out'});
    animation.onfinish = () => el.remove();
    animation.oncancel = () => el.remove();
  }

    function play(mode,x,y,rect) {
      if (mode==='kiss') {
        particle('kiss',x,y,{size:36,dy:-12,duration:450});
        for(let i=0;i<6;i++) particle('heart',x+(Math.random()-.5)*24,y,{size:14+Math.random()*18,dx:(Math.random()-.5)*120,dy:-70-Math.random()*100,rotate:(Math.random()-.5)*40,duration:900+Math.random()*400,delay:i*45});
      } else if(mode==='poke') {
        particle('poke',x,y,{size:45,dx:32,dy:5,rotate:15,duration:500});
        particle('heart',x-12,y-15,{size:17,dx:-18,dy:-65,color:'#eaa179'});
      } else if(mode==='bless' || mode==='shazi') {
        particle(mode,rect.left+rect.width/2,rect.top+rect.height/2,{size:Math.min(rect.width*.75,rect.height*.7,360),dy:0,duration:1400,color:mode==='shazi'?'#ffffff':'#ee6594'});
      } else if(mode==='pet') {
        particle('pet',x,y,{size:43,dx:12,dy:-12,rotate:-15,duration:450,color:'#c79670'});
        for(let i=0;i<3;i++) particle('sparkle',x+(i-1)*18,y-12,{size:12+i*3,dx:(i-1)*32,dy:-45-i*15,color:'#e7b05a',delay:i*50});
      }
    }
    function clear() {
      for(const node of Array.from(effects.children)) {
        for(const animation of node.getAnimations()) animation.cancel();
        node.remove();
      }
    }
    return {play,clear};
  }
  return {actions,svg,createEffects};
})();
