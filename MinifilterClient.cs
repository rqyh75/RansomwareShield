using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace antiRansomware
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MinifilterNotification
    {
        public uint ProcessId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string ProcessName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Action;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Response;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 520)]
        public string TargetPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILTER_MESSAGE_HEADER
    {
        public uint ReplyLength;
        public ulong MessageId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MinifilterMessagePacket
    {
        public FILTER_MESSAGE_HEADER Header;
        public MinifilterNotification Data;
    }

    internal static class FltLibNative
    {
        [DllImport("FltLib.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int FilterConnectCommunicationPort(
            string lpPortName,
            uint dwOptions,
            IntPtr lpContext,
            ushort wSizeOfContext,
            IntPtr lpSecurityAttributes,
            out IntPtr hPort
        );

        [DllImport("FltLib.dll", SetLastError = true)]
        public static extern int FilterGetMessage(
            IntPtr hPort,
            IntPtr lpMessageBuffer,
            uint dwMessageBufferSize,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
    }

    public sealed class MinifilterMessageClient : IDisposable
    {
        private const string PortName = @"\MiniRansomPort";
        private IntPtr _port = IntPtr.Zero;
        private bool _running;

        public event Action<MinifilterNotification>? OnMessageReceived;

        public void Start()
        {
            if (_running) return;

            int hr = FltLibNative.FilterConnectCommunicationPort(
                PortName,
                0,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                out _port
            );

            if (hr != 0)
                throw new Win32Exception(hr, "Failed to connect to minifilter communication port");

            _running = true;
            _ = Task.Run(ListenLoop);
        }

        private void ListenLoop()
        {
            int size = Marshal.SizeOf<MinifilterMessagePacket>();
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try
            {
                while (_running)
                {
                    Marshal.StructureToPtr(new MinifilterMessagePacket(), buffer, false);

                    int hr = FltLibNative.FilterGetMessage(
                        _port,
                        buffer,
                        (uint)size,
                        IntPtr.Zero
                    );

                    if (!_running)
                        break;

                    if (hr == 0)
                    {
                        var packet = Marshal.PtrToStructure<MinifilterMessagePacket>(buffer);
                        OnMessageReceived?.Invoke(packet.Data);
                    }
                    else
                    {
                        Console.WriteLine($"[Minifilter] FilterGetMessage failed: 0x{hr:X8}");
                        Thread.Sleep(200);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            _running = false;

            if (_port != IntPtr.Zero)
            {
                FltLibNative.CloseHandle(_port);
                _port = IntPtr.Zero;
            }
        }
    }
}