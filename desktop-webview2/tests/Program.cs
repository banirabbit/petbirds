using System;
using PetbirdsLite;

internal static class Tests
{
    private static int shown, hidden;
    private static InteractionState last;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Main()
    {
        var controller = new StateController(() => shown++, () => hidden++, state => last = state);
        controller.Start("kiss"); Check(shown == 0 && controller.Mode == null, "启动完成前不能接管桌面");
        controller.UpdateDesktop(false); controller.Start("pet"); Check(shown == 0, "其他应用前台时不能接管鼠标");
        controller.UpdateDesktop(true);
        foreach (var mode in new[] { "poke", "kiss", "shazi", "pet", "bless" }) { controller.Start(mode); Check(controller.Mode == mode, "五种模式都能选择"); }
        controller.Start("invalid"); Check(controller.Mode == "bless", "拒绝未知动作");
        controller.Stop(); Check(hidden == 1 && last.mode == null, "结束按钮隐藏互动层并清空动作");
        controller.Start("kiss"); controller.UpdateDesktop(false); Check(hidden == 2 && controller.Mode == null, "切换应用结束互动");
        controller.UpdateDesktop(true); controller.Start("pet"); controller.Unavailable(); Check(!controller.Ready && controller.Mode == null, "异常时恢复桌面");
        controller.Start("kiss"); Check(controller.Mode == null, "异常后阻止再次进入");
        Console.WriteLine("PASS: desktop gating, five modes, stop button, app switching, failure recovery.");
    }
}
