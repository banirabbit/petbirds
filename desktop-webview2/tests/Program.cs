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
        controller.Start("kiss"); Check(shown == 0 && controller.Mode == null, "挂载完成前不可互动");
        controller.Initialize(); Check(controller.Ready && last.ready, "挂载完成后可用");
        foreach (var mode in new[] { "poke", "kiss", "shazi", "pet", "bless" }) { controller.Start(mode); Check(controller.Mode == mode, "五种模式可选"); }
        Check(shown == 5, "动作不再依赖前台应用");
        controller.Start("invalid"); Check(controller.Mode == "bless", "拒绝未知动作");
        controller.Stop(); Check(hidden == 1 && last.mode == null && controller.Ready, "右键只结束互动");
        controller.Start("pet"); controller.Unavailable(); Check(!controller.Ready && controller.Mode == null && hidden == 2, "挂载失败关闭互动层");
        controller.Start("kiss"); Check(controller.Mode == null, "错误状态阻止互动");
        Console.WriteLine("PASS: attachment readiness, five actions, right-click stop, failure recovery.");
    }
}
