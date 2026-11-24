using System;
using System.Collections.Generic;

namespace PetbirdsLite
{
    internal sealed class InteractionState
    {
        public string mode { get; set; }
        public bool ready { get; set; }
        public bool desktop { get; set; }
        public string message { get; set; } = "";
    }

    internal sealed class StateController
    {
        private static readonly HashSet<string> Modes = new HashSet<string> { "poke", "kiss", "shazi", "pet", "bless" };
        private readonly Action show;
        private readonly Action hide;
        private readonly Action<InteractionState> broadcast;
        public string Mode { get; private set; }
        public bool Ready { get; private set; }
        public bool Desktop { get; private set; }

        public StateController(Action show, Action hide, Action<InteractionState> broadcast)
        {
            this.show = show;
            this.hide = hide;
            this.broadcast = broadcast;
        }

        public InteractionState Snapshot(string message = "") => new InteractionState { mode = Mode, ready = Ready, desktop = Desktop, message = message };

        public void UpdateDesktop(bool desktop)
        {
            Ready = true;
            if (Desktop == desktop) return;
            Desktop = desktop;
            if (!desktop) Stop();
            else broadcast(Snapshot());
        }

        public void Start(string mode)
        {
            if (mode == null || !Modes.Contains(mode)) return;
            if (!Ready || !Desktop)
            {
                broadcast(Snapshot(Ready ? "请先回到桌面，再选择动作" : "正在连接桌面，请稍候"));
                return;
            }
            try { show(); Mode = mode; broadcast(Snapshot()); }
            catch { Mode = null; hide(); throw; }
        }

        public void Stop()
        {
            Mode = null;
            hide();
            broadcast(Snapshot());
        }

        public void Unavailable()
        {
            Ready = false;
            Desktop = false;
            Mode = null;
            hide();
            broadcast(Snapshot("桌面连接中断，请退出并重新打开"));
        }
    }
}
