﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.math.raw
{
    internal abstract class Interleave
    {
        private const ulong M32 = 0x55555555UL;
        private const ulong M64 = 0x5555555555555555UL;
        private const ulong M64R = 0xAAAAAAAAAAAAAAAAUL;

        /*
         * This expands 8 bit indices into 16 bit contents (high bit 14), by inserting 0s between bits.
         * In a binary field, this operation is the same as squaring an 8 bit number.
         */
        //private static readonly ushort[] INTERLEAVE2_TABLE = new ushort[]
        //{
        //    0x0000, 0x0001, 0x0004, 0x0005, 0x0010, 0x0011, 0x0014, 0x0015,
        //    0x0040, 0x0041, 0x0044, 0x0045, 0x0050, 0x0051, 0x0054, 0x0055,
        //    0x0100, 0x0101, 0x0104, 0x0105, 0x0110, 0x0111, 0x0114, 0x0115,
        //    0x0140, 0x0141, 0x0144, 0x0145, 0x0150, 0x0151, 0x0154, 0x0155,
        //    0x0400, 0x0401, 0x0404, 0x0405, 0x0410, 0x0411, 0x0414, 0x0415,
        //    0x0440, 0x0441, 0x0444, 0x0445, 0x0450, 0x0451, 0x0454, 0x0455,
        //    0x0500, 0x0501, 0x0504, 0x0505, 0x0510, 0x0511, 0x0514, 0x0515,
        //    0x0540, 0x0541, 0x0544, 0x0545, 0x0550, 0x0551, 0x0554, 0x0555,
        //    0x1000, 0x1001, 0x1004, 0x1005, 0x1010, 0x1011, 0x1014, 0x1015,
        //    0x1040, 0x1041, 0x1044, 0x1045, 0x1050, 0x1051, 0x1054, 0x1055,
        //    0x1100, 0x1101, 0x1104, 0x1105, 0x1110, 0x1111, 0x1114, 0x1115,
        //    0x1140, 0x1141, 0x1144, 0x1145, 0x1150, 0x1151, 0x1154, 0x1155,
        //    0x1400, 0x1401, 0x1404, 0x1405, 0x1410, 0x1411, 0x1414, 0x1415,
        //    0x1440, 0x1441, 0x1444, 0x1445, 0x1450, 0x1451, 0x1454, 0x1455,
        //    0x1500, 0x1501, 0x1504, 0x1505, 0x1510, 0x1511, 0x1514, 0x1515,
        //    0x1540, 0x1541, 0x1544, 0x1545, 0x1550, 0x1551, 0x1554, 0x1555,
        //    0x4000, 0x4001, 0x4004, 0x4005, 0x4010, 0x4011, 0x4014, 0x4015,
        //    0x4040, 0x4041, 0x4044, 0x4045, 0x4050, 0x4051, 0x4054, 0x4055,
        //    0x4100, 0x4101, 0x4104, 0x4105, 0x4110, 0x4111, 0x4114, 0x4115,
        //    0x4140, 0x4141, 0x4144, 0x4145, 0x4150, 0x4151, 0x4154, 0x4155,
        //    0x4400, 0x4401, 0x4404, 0x4405, 0x4410, 0x4411, 0x4414, 0x4415,
        //    0x4440, 0x4441, 0x4444, 0x4445, 0x4450, 0x4451, 0x4454, 0x4455,
        //    0x4500, 0x4501, 0x4504, 0x4505, 0x4510, 0x4511, 0x4514, 0x4515,
        //    0x4540, 0x4541, 0x4544, 0x4545, 0x4550, 0x4551, 0x4554, 0x4555,
        //    0x5000, 0x5001, 0x5004, 0x5005, 0x5010, 0x5011, 0x5014, 0x5015,
        //    0x5040, 0x5041, 0x5044, 0x5045, 0x5050, 0x5051, 0x5054, 0x5055,
        //    0x5100, 0x5101, 0x5104, 0x5105, 0x5110, 0x5111, 0x5114, 0x5115,
        //    0x5140, 0x5141, 0x5144, 0x5145, 0x5150, 0x5151, 0x5154, 0x5155,
        //    0x5400, 0x5401, 0x5404, 0x5405, 0x5410, 0x5411, 0x5414, 0x5415,
        //    0x5440, 0x5441, 0x5444, 0x5445, 0x5450, 0x5451, 0x5454, 0x5455,
        //    0x5500, 0x5501, 0x5504, 0x5505, 0x5510, 0x5511, 0x5514, 0x5515,
        //    0x5540, 0x5541, 0x5544, 0x5545, 0x5550, 0x5551, 0x5554, 0x5555
        //};

        internal static uint Expand8to16(uint x)
        {
            x &= 0xFFU;
            x = (x | (x << 4)) & 0x0F0FU;
            x = (x | (x << 2)) & 0x3333U;
            x = (x | (x << 1)) & 0x5555U;
            return x;
        }

        internal static uint Expand16to32(uint x)
        {
            x &= 0xFFFFU;
            x = (x | (x << 8)) & 0x00FF00FFU;
            x = (x | (x << 4)) & 0x0F0F0F0FU;
            x = (x | (x << 2)) & 0x33333333U;
            x = (x | (x << 1)) & 0x55555555U;
            return x;
        }

        internal static ulong Expand32to64(uint x)
        {
            // "shuffle" low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x0000FF00U, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F0U, 4);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0CU, 2);
            x = Bits.BitPermuteStep(x, 0x22222222U, 1);

            return ((x >> 1) & M32) << 32 | (x & M32);
        }

        internal static void Expand64To128(ulong x, ulong[] z, int zOff)
        {
            // "shuffle" low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x00000000FFFF0000UL, 16);
            x = Bits.BitPermuteStep(x, 0x0000FF000000FF00UL, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F000F000F0UL, 4);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0C0C0C0C0CUL, 2);
            x = Bits.BitPermuteStep(x, 0x2222222222222222UL, 1);

            z[zOff] = (x) & M64;
            z[zOff + 1] = (x >> 1) & M64;
        }

        internal static void Expand64To128(ulong[] xs, int xsOff, int xsLen, ulong[] zs, int zsOff)
        {
            for (int i = 0; i < xsLen; ++i)
            {
                Expand64To128(xs[xsOff + i], zs, zsOff);
                zsOff += 2;
            }
        }

        internal static void Expand64To128Rev(ulong x, ulong[] z, int zOff)
        {
            // "shuffle" low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x00000000FFFF0000UL, 16);
            x = Bits.BitPermuteStep(x, 0x0000FF000000FF00UL, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F000F000F0UL, 4);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0C0C0C0C0CUL, 2);
            x = Bits.BitPermuteStep(x, 0x2222222222222222UL, 1);

            z[zOff] = (x) & M64R;
            z[zOff + 1] = (x << 1) & M64R;
        }

        internal static uint Shuffle(uint x)
        {
            // "shuffle" low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x0000FF00U, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F0U, 4);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0CU, 2);
            x = Bits.BitPermuteStep(x, 0x22222222U, 1);
            return x;
        }

        internal static ulong Shuffle(ulong x)
        {
            // "shuffle" low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x00000000FFFF0000UL, 16);
            x = Bits.BitPermuteStep(x, 0x0000FF000000FF00UL, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F000F000F0UL, 4);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0C0C0C0C0CUL, 2);
            x = Bits.BitPermuteStep(x, 0x2222222222222222UL, 1);
            return x;
        }

        internal static uint Shuffle2(uint x)
        {
            // "shuffle" (twice) low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x00AA00AAU, 7);
            x = Bits.BitPermuteStep(x, 0x0000CCCCU, 14);
            x = Bits.BitPermuteStep(x, 0x00F000F0U, 4);
            x = Bits.BitPermuteStep(x, 0x0000FF00U, 8);
            return x;
        }

        internal static uint Unshuffle(uint x)
        {
            // "unshuffle" even bits to low half and odd bits to high half
            x = Bits.BitPermuteStep(x, 0x22222222U, 1);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0CU, 2);
            x = Bits.BitPermuteStep(x, 0x00F000F0U, 4);
            x = Bits.BitPermuteStep(x, 0x0000FF00U, 8);
            return x;
        }

        internal static ulong Unshuffle(ulong x)
        {
            // "unshuffle" even bits to low half and odd bits to high half
            x = Bits.BitPermuteStep(x, 0x2222222222222222UL, 1);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0C0C0C0C0CUL, 2);
            x = Bits.BitPermuteStep(x, 0x00F000F000F000F0UL, 4);
            x = Bits.BitPermuteStep(x, 0x0000FF000000FF00UL, 8);
            x = Bits.BitPermuteStep(x, 0x00000000FFFF0000UL, 16);
            return x;
        }

        internal static uint Unshuffle2(uint x)
        {
            // "unshuffle" (twice) even bits to low half and odd bits to high half
            x = Bits.BitPermuteStep(x, 0x0000FF00U, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F0U, 4);
            x = Bits.BitPermuteStep(x, 0x0000CCCCU, 14);
            x = Bits.BitPermuteStep(x, 0x00AA00AAU, 7);
            return x;
        }
    }
}
