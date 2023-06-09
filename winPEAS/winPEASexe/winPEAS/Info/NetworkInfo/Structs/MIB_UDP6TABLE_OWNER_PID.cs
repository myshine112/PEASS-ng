﻿using System.Runtime.InteropServices;

namespace winPEAS.Info.NetworkInfo.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDP6TABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
        public MIB_UDP6ROW_OWNER_PID[] table;
    }
}
