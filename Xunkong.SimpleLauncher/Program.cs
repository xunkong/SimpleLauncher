using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Xunkong.Hoyolab;




string? genshinGamePath = null;
string? genshinLauncherPath = null;
bool? popupWindow = null;
bool? fullScreen = null;
uint? width = null;
uint? height = null;
string? cookie = null;
double homeCoinNotifyRatio = 0.8;

const string REG_PATH = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\原神\";
const string REG_KEY = "InstallPath";




AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
var timer = new System.Timers.Timer(30000);
timer.Elapsed += Timer_Elapsed;

var ps = Process.GetProcessesByName("YuanShen").Concat(Process.GetProcessesByName("GenshinImpact"));
if (ps.Any())
{
    await SendNotificationAsync("出错了", "已有游戏进程在运行");
    return;
}



// 加载配置
var baseDir = AppContext.BaseDirectory;
var configPath = Path.Combine(baseDir, "Config.txt");
if (File.Exists(configPath))
{
    var configLines = await File.ReadAllLinesAsync(configPath);
    foreach (var configLine in configLines)
    {
        if (configLine.StartsWith("#"))
        {
            continue;
        }
        var key = configLine.Split("=").FirstOrDefault();
        var value = configLine.Replace($"{key}=", "");
        switch (key)
        {
            case "GenshinGamePath":
                genshinGamePath = value;
                break;
            case "GenshinLauncherPath":
                genshinLauncherPath = value;
                break;
            case "IsPopupWindow":
                if (bool.TryParse(value, out var result1))
                {
                    popupWindow = result1;
                }
                break;
            case "IsFullScreen":
                if (bool.TryParse(value, out var result2))
                {
                    fullScreen = result2;
                }
                break;
            case "Width":
                if (uint.TryParse(value, out var result3))
                {
                    width = result3;
                }
                break;
            case "Height":
                if (uint.TryParse(value, out var result4))
                {
                    height = result4;
                }
                break;
            case "Cookie":
                cookie = value;
                break;
            case "HomeCoinNotifyRatio":
                if (double.TryParse(value, out var result5))
                {
                    homeCoinNotifyRatio = result5;
                }
                break;
            default:
                break;
        }
    }
}



if (IsAdministrator())
{
    // 运行原神启动器
    if (!File.Exists(genshinLauncherPath))
    {
        genshinLauncherPath = Registry.GetValue(REG_PATH, REG_KEY, null) as string;
    }
    if (File.Exists(genshinLauncherPath))
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = genshinLauncherPath,
            UseShellExecute = true,
            Verb = "runas",
        });
    }
    else
    {
        await SendNotificationAsync("出错了", "无法找到原神启动器文件");
        return;
    }
}
else
{
    // 运行原神游戏
    if (!File.Exists(genshinGamePath))
    {
        genshinLauncherPath = Registry.GetValue(REG_PATH, REG_KEY, null) as string;
        if (!string.IsNullOrWhiteSpace(genshinLauncherPath))
        {
            var genshinConfigPath = Path.Combine(genshinLauncherPath, "config.ini");
            if (File.Exists(configPath))
            {
                var str = await File.ReadAllTextAsync(genshinConfigPath);
                var gamePath = Regex.Match(str, @"game_install_path=(.+)").Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(gamePath))
                {
                    genshinGamePath = Path.Combine(gamePath, "YuanShen.exe");
                }
            }
        }
    }
    if (File.Exists(genshinGamePath))
    {
        var sb = new StringBuilder();
        if (fullScreen ?? false)
        {
            sb.Append("-screen-fullscreen ");
        }
        else if (popupWindow ?? false)
        {
            sb.Append("-popupwindow ");
            if (width is not null && height is not null && width * height > 0)
            {
                sb.Append($"-screen-width {width} -screen-height {height}");
            }
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = genshinGamePath,
            UseShellExecute = true,
            Arguments = sb.ToString(),
            Verb = "runas",
        });
    }
    else
    {
        await SendNotificationAsync("出错了", "无法找到原神游戏文件");
        return;
    }
}


if (!string.IsNullOrWhiteSpace(cookie))
{
    var client = new HoyolabClient();
    var roles = await client.GetGenshinRoleInfosAsync(cookie);
    bool showNotification = false;
    var sb = new StringBuilder();
    foreach (var role in roles)
    {
        bool homeCoin = false, transformer = false;
        var dailyNote = await client.GetDailyNoteAsync(role);
        if (dailyNote.CurrentHomeCoin > dailyNote.MaxHomeCoin * homeCoinNotifyRatio)
        {
            homeCoin = true;
        }
        if (dailyNote.Transformer?.RecoveryTime?.Reached ?? false)
        {
            transformer = true;
        }
        if (homeCoin || transformer)
        {
            showNotification = true;
            sb.AppendLine($"{role.Nickname}：{(homeCoin ? "洞天宝钱 " : "")}{(transformer ? "参量质变仪" : "")}");
        }
    }
    if (showNotification)
    {
        await SendNotificationAsync("注意了", sb.ToString());
    }
}








async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    if (e.ExceptionObject is Win32Exception win32Ex)
    {
        if (win32Ex.NativeErrorCode == 1223)
        {
            return;
        }
    }
    if (e.ExceptionObject is Exception ex)
    {
        await SendNotificationAsync("出错了", $"{ex.GetType().Name}\n{ex.Message}");
    }
    else
    {
        await SendNotificationAsync("出错了", "不知道什么错误");
    }
}


async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
{
    await SendNotificationAsync("出错了", "执行超时");
    Environment.Exit(1);
}






static bool IsAdministrator()
{
    WindowsIdentity identity = WindowsIdentity.GetCurrent();
    WindowsPrincipal principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}




static async Task SendNotificationAsync(string title, string? message = null)
{
    try
    {
        var icon = Path.Combine(AppContext.BaseDirectory, "Icon.png");
        const string key = @"HKEY_CURRENT_USER\Software\Classes\AppUserModelId\Xunkong.SimpleLauncher";
        Registry.SetValue(key, "DisplayName", "简单启动器", RegistryValueKind.String);
        Registry.SetValue(key, "IconBackgroundColor", "FFDDDDDD", RegistryValueKind.String);
        Registry.SetValue(key, "IconUri", icon, RegistryValueKind.String);
        var toastContent = $"""
            <toast>
                <visual>
                    <binding template="ToastGeneric">
                        <text>{title}</text>
                        <text>{message}</text>
                    </binding>
                </visual>
            </toast>
            """;
        var doc = new XmlDocument();
        doc.LoadXml(toastContent);
        var noti = new ToastNotification(doc);
        ToastNotificationManager.CreateToastNotifier("Xunkong.SimpleLauncher").Show(noti);
    }
    finally
    {
        // 等通知发出去
        await Task.Delay(1000);
    }
}
