﻿using System.Runtime.InteropServices;

namespace winPEAS.Info.NetworkInfo.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
        public MIB_UDPROW_OWNER_PID[] table;
    }
}
