using BetterWin32Errors;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Injector;

class Native
{
    public const string kernel = "kernel32.dll";

    [DllImport("shell32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsUserAnAdmin();

    [DllImport(kernel, SetLastError = true, ExactSpelling = true)]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess,
                    IntPtr lpAddress,
                    int dwSize,
                    AllocationType flAllocationType,
                    MemoryProtection flProtect);

    [DllImport(kernel, SetLastError = true)]
    static extern bool WriteProcessMemory(
      IntPtr hProcess,
      IntPtr lpBaseAddress,
      byte[] lpBuffer,
      int nSize,
      out int lpNumberOfBytesWritten);

    [DllImport(kernel)]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess,
       IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
       IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [DllImport(kernel, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport(kernel, CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

    [DllImport(kernel, SetLastError = true)]
    static extern IntPtr OpenProcess(
        ProcessAccessFlags processAccess,
        bool bInheritHandle,
        uint processId);

    public static ProcessHandle OpenProcess(Process proc, ProcessAccessFlags flags)
    {
        return new ProcessHandle(OpenProcess(flags, false, (uint)proc.Id));
    }

    public static FunctionHandle GetFunctionAddress(string library, string function)
    {
        return new FunctionHandle(GetProcAddress(GetModuleHandle(library), function));
    }

    static MemoryHandle AllocateMemoryForWriteInProcess(ProcessHandle processHandle, int length)
    {
        return new MemoryHandle(VirtualAllocEx(processHandle.Pointer, IntPtr.Zero, length, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite));
    }

    public static MemoryHandle AllocateAndWriteMemoryToProcess(ProcessHandle processHandle, byte[] data)
    {
        var memory = AllocateMemoryForWriteInProcess(processHandle, data.Length);
        if (memory.IsNull)
        {
            throw new Win32Exception("Error during memory allocation");
        }
        if (!WriteProcessMemory(processHandle.Pointer, memory.Pointer, data, data.Length, out var bytesOfDataWritten))
        {
            throw new Win32Exception("Writing memory to procces failed");
        }
        if (bytesOfDataWritten != data.Length)
        {
            throw new Win32Exception("Written unexpected number of bytes");
        }
        return memory;
    }

    public static void CreateThreadInProcess(ProcessHandle processHandle, FunctionHandle function, MemoryHandle memory)
    {
        if (CreateRemoteThread(processHandle.Pointer, IntPtr.Zero, 0, function.Pointer, memory.Pointer, 0, out var _) == IntPtr.Zero)
        {
            throw new Win32Exception("Error creating remote thread");
        }
    }
}

record ProcessHandle(IntPtr Pointer);

record FunctionHandle(IntPtr Pointer);

record MemoryHandle(IntPtr Pointer)
{
    public bool IsNull { get => Pointer == IntPtr.Zero; }
}