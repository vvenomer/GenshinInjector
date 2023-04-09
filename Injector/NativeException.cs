using BetterWin32Errors;

namespace Injector;

public class NativeException : Exception
{
    public NativeException(Win32Exception win32Exception) 
        : base($"{win32Exception.CustomMessage}. System error: {win32Exception.Message}", win32Exception) { }
}