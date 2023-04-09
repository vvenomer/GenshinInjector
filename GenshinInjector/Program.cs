using BetterWin32Errors;
using Injector;

if(args.Length != 1 || args[0] == "--help")
{
    Console.WriteLine("Program usage: GenshinInjector.exe dllPath\n\tdllPath - path to dll to be injected into Genshin");
    Environment.Exit(0);
}
if(!File.Exists(args[0]))
{
    throw new FileNotFoundException($"File {args[0]} doesn't exist");
}
Console.WriteLine($"Injecting {args[0]} into GensinImpact");
try
{
    Inject.InjectDll("GenshinImpact", args[0]);
    Console.WriteLine("Injected successfully");
} catch (NativeException e)
{
    Console.Error.WriteLine(e.Message);
    if (e.InnerException is Win32Exception { Error: Win32Error.ERROR_ACCESS_DENIED })
    {
        Console.Error.WriteLine("This error might happen if Genshin has already loaded. Make sure to run injector first and then Genshin");
    }
    Environment.Exit(1);
}
