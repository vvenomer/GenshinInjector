using BetterWin32Errors;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace Injector;

public class Inject
{
    static TimeSpan WaitTime { get; set; } = TimeSpan.FromMilliseconds(500);
    static void AssertIsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            throw new UnauthorizedAccessException("You have to run this as admin");
        }
    }

    static ProcessHandle FindProcess(string processName)
    {
        var flags = Enum.GetValues<ProcessAccessFlags>().Aggregate((a, b) => a | b); //all flags combined - overkill
        while (true)
        {
            try
            {
                return Native.OpenProcess(Process.GetProcessesByName(processName).Single(), flags);
            }
            catch (InvalidOperationException e)
            {
                if (e.Message == "Sequence contains more than one element")
                {
                    throw new InvalidOperationException($"There is more then one process named {processName}");
                }
            }
            Console.WriteLine($"Process not found. Waiting {WaitTime.TotalSeconds}s");
            Thread.Sleep(WaitTime);
        }
    }

    /// <summary>
    /// Injects DLL provided in dllPath to process named processName
    /// </summary>
    /// <param name="processName"></param>
    /// <param name="dllPath"></param>
    /// <exception cref="NativeException"></exception>
    public static void InjectDll(string processName, string dllPath)
    {
        new Inject()
            .AddDllToInject(dllPath)
            .InjectDlls(processName);
    }

    private readonly List<byte[]> dllPathsBytes = new();
    public Inject()
    {
        AssertIsAdmin();
    }

    /// <summary>
    /// Adds DLL to be injected later with call to InjectDlls
    /// </summary>
    /// <param name="dllPath"></param>
    /// <returns>self</returns>
    public Inject AddDllToInject(string dllPath)
    {
        var dllPathBytes = Encoding.Default.GetBytes(Path.GetFullPath(dllPath));
        dllPathsBytes.Add(dllPathBytes);
        return this;
    }


    /// <summary>
    /// Injects previously provided DLLs to process named processName
    /// </summary>
    /// <param name="processName"></param>
    /// <exception cref="NativeException"></exception>
    public void InjectDlls(string processName)
    {
        try
        {
            var process = FindProcess(processName);

            var loadLib = Native.GetFunctionAddress(Native.kernel, "LoadLibraryA");

            foreach (var dllPathBytes in dllPathsBytes)
            {
                var memory = Native.AllocateAndWriteMemoryToProcess(process, dllPathBytes);

                Native.CreateThreadInProcess(process, loadLib, memory);
            }
        }
        catch (Win32Exception e)
        {
            throw new NativeException(e);
        }
    }
}
