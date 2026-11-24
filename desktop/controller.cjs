'use strict';
const modes = new Set(['poke', 'kiss', 'shazi', 'pet', 'bless']);

class InteractionController {
  constructor(view) {
    this.view = view;
    this.mode = null;
    this.ready = false;
    this.desktop = false;
  }
  snapshot(message = '') {
    return {mode:this.mode, ready:this.ready, desktop:this.desktop, message};
  }
  updateDesktop(desktop) {
    this.ready = true;
    this.desktop = desktop;
    if (!desktop) this.stop();
    else this.view.broadcast(this.snapshot());
  }
  start(mode) {
    if (!modes.has(mode)) return;
    if (!this.ready || !this.desktop) {
      this.view.broadcast(this.snapshot(this.ready ? '请先回到桌面，再选择动作' : '正在连接桌面，请稍候'));
      return;
    }
    this.mode = mode;
    this.view.show();
    this.view.broadcast(this.snapshot());
  }
  stop() {
    this.mode = null;
    this.view.hide();
    this.view.broadcast(this.snapshot());
  }
  unavailable() {
    this.ready = false;
    this.desktop = false;
    this.mode = null;
    this.view.hide();
    this.view.broadcast(this.snapshot('桌面连接中断，请退出并重新打开'));
  }
}
module.exports = {InteractionController};
