using System;
using PetbirdsLite;

internal static class Tests
{
    private static int shown, hidden;
    private static InteractionState last;
    private static bool toolbarVisible;
    private static int visibilityChanges;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Main()
    {
        var controller = new StateController(() => shown++, () => hidden++, state => last = state, visible => { toolbarVisible = visible; visibilityChanges++; });
        controller.Start("kiss"); Check(shown == 0 && controller.Mode == null, "启动完成前不能接管桌面");
        controller.UpdateDesktop(false); controller.Start("pet"); Check(shown == 0, "其他应用前台时不能接管鼠标");
        Check(!toolbarVisible, "其他应用前台时工具条隐藏");
        controller.UpdateDesktop(true);
        Check(toolbarVisible, "回到桌面后自动显示工具条");
        var stableChanges = visibilityChanges;
        controller.UpdateDesktop(true); Check(visibilityChanges == stableChanges, "相同桌面状态不反复抬升窗口");
        foreach (var mode in new[] { "poke", "kiss", "shazi", "pet", "bless" }) { controller.Start(mode); Check(controller.Mode == mode, "五种模式都能选择"); }
        controller.Start("invalid"); Check(controller.Mode == "bless", "拒绝未知动作");
        var beforeStop = hidden;
        controller.Stop(); Check(hidden == beforeStop + 1 && last.mode == null && toolbarVisible, "右键结束后清空动作，桌面工具条保留");
        controller.Start("kiss"); controller.UpdateDesktop(false); Check(hidden == beforeStop + 2 && controller.Mode == null && !toolbarVisible, "切换应用结束互动并隐藏工具条");
        controller.UpdateDesktop(true); Check(toolbarVisible && controller.Mode == null, "返回桌面只恢复工具条，不自动开始互动");
        controller.Start("pet"); controller.Unavailable(); Check(!controller.Ready && controller.Mode == null && !toolbarVisible, "异常时恢复桌面并隐藏工具条");
        controller.Start("kiss"); Check(controller.Mode == null, "异常后阻止再次进入");
        Console.WriteLine("PASS: desktop-only toolbar, five modes, right-click stop, app switching, failure recovery.");
    }
}
