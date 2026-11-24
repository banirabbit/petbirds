$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class PetbirdsDesktop {
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);
    public static bool IsDesktop() {
        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return false;
        var text = new StringBuilder(256);
        GetClassName(handle, text, text.Capacity);
        string name = text.ToString();
        return name == "Progman" || name == "WorkerW";
    }
}
'@
while ($true) {
    if ([PetbirdsDesktop]::IsDesktop()) { [Console]::WriteLine('desktop') }
    else { [Console]::WriteLine('other') }
    [Console]::Out.Flush()
    Start-Sleep -Milliseconds 200
}
