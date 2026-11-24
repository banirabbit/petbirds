using System;

namespace PetbirdsLite
{
    internal sealed class InteractionAction
    {
        public string Id { get; }
        public string Label { get; }
        private InteractionAction(string id, string label) { Id = id; Label = label; }
        public static readonly InteractionAction[] All = {
            new InteractionAction("poke", "戳一戳"), new InteractionAction("kiss", "亲亲"),
            new InteractionAction("shazi", "啥子"), new InteractionAction("pet", "摸摸"), new InteractionAction("bless", "祝福")
        };
    }
    internal sealed class InteractionState
    {
        public string mode { get; set; }
        public bool ready { get; set; }
        public string message { get; set; } = "";
    }

    internal sealed class StateController
    {
        private readonly Action show;
        private readonly Action hide;
        private readonly Action<InteractionState> broadcast;
        public string Mode { get; private set; }
        public bool Ready { get; private set; }

        public StateController(Action show, Action hide, Action<InteractionState> broadcast)
        {
            this.show = show;
            this.hide = hide;
            this.broadcast = broadcast;
        }

        public InteractionState Snapshot(string message = "") => new InteractionState { mode = Mode, ready = Ready, message = message };
        public void Initialize() { Ready = true; broadcast(Snapshot()); }
        public void Start(string mode)
        {
            if (!Array.Exists(InteractionAction.All, action => action.Id == mode)) return;
            if (!Ready) { broadcast(Snapshot("互动效果正在准备，请稍候")); return; }
            try { show(); Mode = mode; broadcast(Snapshot()); }
            catch { Mode = null; hide(); throw; }
        }
        public void Stop() { Mode = null; hide(); broadcast(Snapshot()); }
        public void Unavailable()
        {
            Ready = false;
            Mode = null;
            hide();
            broadcast(Snapshot("互动效果已中断，请退出并重新打开"));
        }
    }
}
