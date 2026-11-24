'use strict';
(() => {
  const paths = {
    upload:'<rect x="3" y="5" width="17" height="16" rx="3"/><path d="m4 17 5-5 4 4 3-3 4 4M18 3v6m-3-3h6"/><circle cx="8" cy="9" r="1"/>',
    poke:'<path d="M8 13V4.5c0-2.7 3.8-2.7 3.8 0v5c0-2.1 3-2.1 3 0v1c0-1.9 2.8-1.9 2.8 0v1c0-1.7 2.8-1.7 2.8 0v3.7c0 2.7-1.4 4.7-3.2 6H9c-1.2-2-2.3-3.6-3.6-5l-1.5-1.6c-1.8-1.9.5-4.1 2.3-2.5L8 14" fill="#fffaf6" stroke-width="1.6"/><path d="M11.8 9.5v3m3-2v2.2m2.8-1.2v1.7" stroke-width="1.3"/><ellipse cx="14.6" cy="16.7" rx="2.7" ry="1.8" fill="#f7c8d7" stroke="none"/><path d="M9 21.2h8.2" stroke="#ed9eb9" stroke-width="2.4"/>',
    kiss:'<path d="M2 12c4-2 5-8 10-4 5-4 6 2 10 4-4 3-6 7-10 7S5 15 2 12Z"/><path d="M2 12c4 0 6 2 10 1 4 1 6-1 10-1"/>',
    hug:'<path d="M3 10c-3 6 1 11 9 11s12-5 9-11M3 10l5 5m13-5-5 5M3 17c2-1 5-1 9 2 4-3 7-3 9-2"/><path d="M12 12 7 7C3 2 10 0 12 4c2-4 9-2 5 3Z" fill="currentColor" stroke="none"/>',
    bless:'<text x="12" y="12" text-anchor="middle" dominant-baseline="central" font-family="Apple Color Emoji,Segoe UI Emoji,Noto Color Emoji,sans-serif" font-size="21" fill="currentColor" stroke="none">㊗️</text>',
    pet:'<path d="m5 20 1-7c0-2 2-2 2 0l1-5c0-2 3-2 3 0l-1 5 2-7c1-2 3-1 2 1l-1 7 2-5c1-2 3-1 2 1l-1 5c3-2 5 0 3 2l-4 4Z" fill="#ffffff" stroke="#66544c"/><path d="M3 5 1 3m7 0V1m12 3 2-2"/>',
    heart:'<path d="M12 21 3 12C-4 4 7-3 12 5c5-8 16-1 9 7Z" fill="currentColor" stroke="none"/>',
    sparkle:'<path d="m12 2 2.5 7.5L22 12l-7.5 2.5L12 22l-2.5-7.5L2 12l7.5-2.5Z" fill="currentColor" stroke="none"/>',
    paw:'<ellipse cx="12" cy="16" rx="6" ry="5" fill="currentColor" stroke="none"/><g fill="currentColor" stroke="none"><ellipse cx="4" cy="9" rx="2.4" ry="3"/><ellipse cx="9" cy="5" rx="2.3" ry="3"/><ellipse cx="15" cy="5" rx="2.3" ry="3"/><ellipse cx="20" cy="9" rx="2.4" ry="3"/></g>',
    close:'<path d="m6 6 12 12M6 18 18 6"/>'
  };
  const svg = name => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name]}</svg>`;
  const actions = [{id:'poke',label:'戳一戳'},{id:'kiss',label:'亲亲'},{id:'hug',label:'抱抱'},{id:'pet',label:'摸摸'},{id:'bless',label:'祝福'}];
  const pet = document.querySelector('#pet');
  const frame = document.querySelector('#photo-frame');
  const tools = document.querySelector('.tools');
  const list = document.querySelector('#tool-list');
  const toggle = document.querySelector('#menu-toggle');
  const input = document.querySelector('#upload');
  const emptyUpload = document.querySelector('#empty-upload');
  const cursor = document.querySelector('#cursor');
  const effects = document.querySelector('#effects');
  const toast = document.querySelector('#toast');
  const mobile = matchMedia('(max-width:700px)');
  const reduced = matchMedia('(prefers-reduced-motion:reduce)');
  let mode = 'kiss', objectUrl = null, uploadVersion = 0, activePointer = null, lastStroke = 0, toastTimer, photoAnimation;

  function notify(message) {
    toast.textContent = message;
    toast.classList.add('visible');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove('visible'), 2200);
  }
  function menu(open) {
    tools.classList.toggle('open', open);
    toggle.setAttribute('aria-expanded', String(open));
    toggle.setAttribute('aria-label', open ? '收起互动工具' : '展开互动工具');
    toggle.innerHTML = svg(open ? 'close' : mode);
    list.inert = mobile.matches && !open;
  }
  function select(id, announce = false) {
    mode = id;
    list.querySelectorAll('[data-action]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.action === id)));
    cursor.innerHTML = svg(id);
    pet.setAttribute('aria-label', `${actions.find(a => a.id === id).label}小动物，按回车或空格互动`);
    if (mobile.matches) menu(false);
    else toggle.innerHTML = svg(id);
    if (announce) notify(`已切换${actions.find(a => a.id === id).label}`);
  }
  function button(id, label, upload = false) {
    const el = document.createElement('button');
    el.className = 'round' + (upload ? ' upload-button' : '');
    el.type = 'button';
    el.setAttribute('aria-label', label);
    el.dataset.label = label;
    el.innerHTML = svg(id);
    if (!upload) { el.dataset.action = id; el.setAttribute('aria-pressed', 'false'); }
    el.addEventListener('click', () => {
      if (upload) input.click();
      else { select(id, mobile.matches); if (mobile.matches) toggle.focus({preventScroll:true}); }
    });
    list.append(el);
  }
  button('upload','上传图片',true);
  emptyUpload.addEventListener('click', () => input.click());
  actions.forEach(a => button(a.id,a.label));
  toggle.addEventListener('click', () => menu(!tools.classList.contains('open')));
  document.addEventListener('keydown', event => { if (event.key === 'Escape') { menu(false); if (mobile.matches) toggle.focus(); } });
  mobile.addEventListener('change', () => menu(false));
  select(mode);
  menu(false);

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
  function movePhoto(keyframes, duration) {
    if (reduced.matches) return;
    photoAnimation?.cancel();
    photoAnimation = pet.animate(keyframes, {duration,easing:'ease-in-out'});
  }
  function interact(x,y,stroke=false) {
    if (!pet.complete || !pet.naturalWidth) return;
    if (mode === 'kiss') {
      particle('kiss',x,y,{size:36,dy:-12,duration:450});
      for (let i=0;i<6;i++) particle('heart',x+(Math.random()-.5)*24,y,{size:14+Math.random()*18,dx:(Math.random()-.5)*120,dy:-70-Math.random()*100,rotate:(Math.random()-.5)*40,duration:900+Math.random()*400,delay:i*45});
      movePhoto([{transform:'scale(1)'},{transform:'scale(1.012)',offset:.35},{transform:'scale(1)'}],330);
    } else if (mode === 'poke') {
      particle('poke',x,y,{size:45,dx:32,dy:5,rotate:15,duration:500});
      particle('heart',x-12,y-15,{size:17,dx:-18,dy:-65,color:'#eaa179'});
      if (!stroke) movePhoto([{transform:'rotate(0)'},{transform:'rotate(-.6deg)',offset:.3},{transform:'rotate(.6deg)',offset:.7},{transform:'rotate(0)'}],650);
    } else if (mode === 'hug') {
      const rect = pet.getBoundingClientRect();
      particle('hug',rect.left+rect.width/2,rect.top+rect.height*.55,{size:Math.min(rect.width*.58,260),dy:0,duration:1000,color:'#e99caa'});
      [-1,1].forEach(side => particle('heart',x+side*35,y-25,{size:23,dx:side*45,dy:-85,rotate:side*20}));
      movePhoto([{transform:'scale(1)'},{transform:'scale(.975)',offset:.4},{transform:'scale(1)'}],650);
    } else if (mode === 'bless') {
      const rect = pet.getBoundingClientRect();
      particle('bless',rect.left+rect.width/2,rect.top+rect.height/2,{size:Math.min(rect.width*.75,rect.height*.7,360),dy:0,duration:1400,color:'#ee6594'});
      movePhoto([{transform:'scale(1)'},{transform:'scale(1.015)',offset:.4},{transform:'scale(1)'}],550);
    } else {
      particle('pet',x,y,{size:43,dx:12,dy:-12,rotate:-15,duration:450,color:'#c79670'});
      for(let i=0;i<3;i++) particle('sparkle',x+(i-1)*18,y-12,{size:12+i*3,dx:(i-1)*32,dy:-45-i*15,color:'#e7b05a',delay:i*50});
      movePhoto([{transform:'translateX(0)'},{transform:'translateX(-3px)'},{transform:'translateX(3px)'},{transform:'translateX(-2px)'},{transform:'translateX(0)'}],300);
    }
  }
  pet.addEventListener('pointerdown', event => {
    if (!event.isPrimary || (event.pointerType==='mouse' && event.button!==0)) return;
    if (mobile.matches && tools.classList.contains('open')) menu(false);
    activePointer = event.pointerId;
    pet.setPointerCapture(event.pointerId);
    lastStroke = performance.now();
    interact(event.clientX,event.clientY);
  });
  pet.addEventListener('pointermove', event => {
    const fine = event.pointerType === 'mouse';
    cursor.style.transform = `translate(${event.clientX-19}px,${event.clientY-19}px)`;
    cursor.classList.toggle('visible',fine);
    document.body.classList.toggle('custom-cursor',fine);
    if (activePointer !== event.pointerId || !['poke','pet'].includes(mode) || performance.now()-lastStroke<120) return;
    const r = pet.getBoundingClientRect();
    if(event.clientX<r.left || event.clientX>r.right || event.clientY<r.top || event.clientY>r.bottom) return;
    lastStroke = performance.now();
    interact(event.clientX,event.clientY,true);
  });
  const hideCursor = () => { cursor.classList.remove('visible'); document.body.classList.remove('custom-cursor'); };
  pet.addEventListener('pointerleave',hideCursor);
  pet.addEventListener('lostpointercapture',() => {activePointer=null;});
  ['pointerup','pointercancel'].forEach(name => pet.addEventListener(name,() => {activePointer=null;}));
  window.addEventListener('blur',() => {activePointer=null;hideCursor();});
  pet.addEventListener('keydown',event => {
    if (event.key!=='Enter' && event.key!==' ') return;
    event.preventDefault();
    const r=pet.getBoundingClientRect();
    interact(r.left+r.width/2,r.top+r.height*.45);
  });

  input.addEventListener('change', async () => {
    const file = input.files[0];
    input.value = '';
    if (!file) return;
    if (file.size>25*1024*1024) {notify('请选择 25 MB 以内的图片');return;}
    if (!file.type.startsWith('image/')) {notify('请选择图片文件');return;}
    const version = ++uploadVersion;
    const nextUrl = URL.createObjectURL(file);
    const probe = new Image();
    probe.src = nextUrl;
    try {
      await probe.decode();
      if (version!==uploadVersion) {URL.revokeObjectURL(nextUrl);return;}
      photoAnimation?.cancel();
      effects.replaceChildren();
      pet.src = nextUrl;
      pet.hidden = false;
      emptyUpload.hidden = true;
      frame.classList.remove('empty');
      pet.alt = '上传的小动物照片，点击与它互动';
      if(objectUrl) URL.revokeObjectURL(objectUrl);
      objectUrl=nextUrl;
      menu(false);
      notify('图片已换好，开始互动吧');
    } catch {
      URL.revokeObjectURL(nextUrl);
      if(version===uploadVersion) notify('这张图片无法读取，请换一张 JPG、PNG 或 WebP 图片');
    }
  });
  pet.addEventListener('error',()=>notify('图片加载失败，请点击上传选择一张图片'));
})();
