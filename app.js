'use strict';
(() => {
  const {actions,svg}=PetbirdsEffects;
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

  const effectEngine=PetbirdsEffects.createEffects(effects,reduced);
  function movePhoto(keyframes, duration) {
    if (reduced.matches) return;
    photoAnimation?.cancel();
    photoAnimation = pet.animate(keyframes, {duration,easing:'ease-in-out'});
  }
  function interact(x,y,stroke=false) {
    if (!pet.complete || !pet.naturalWidth) return;
    effectEngine.play(mode,x,y,pet.getBoundingClientRect());
    if (mode==='kiss') movePhoto([{transform:'scale(1)'},{transform:'scale(1.012)',offset:.35},{transform:'scale(1)'}],330);
    else if (mode==='poke' && !stroke) movePhoto([{transform:'rotate(0)'},{transform:'rotate(-.6deg)',offset:.3},{transform:'rotate(.6deg)',offset:.7},{transform:'rotate(0)'}],650);
    else if (mode==='bless' || mode==='shazi') movePhoto([{transform:'scale(1)'},{transform:'scale(1.015)',offset:.4},{transform:'scale(1)'}],550);
    else if (mode==='pet') movePhoto([{transform:'translateX(0)'},{transform:'translateX(-3px)'},{transform:'translateX(3px)'},{transform:'translateX(-2px)'},{transform:'translateX(0)'}],300);
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
