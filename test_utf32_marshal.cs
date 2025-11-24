using System;
using System.Runtime.InteropServices;

class Test {
    static IntPtr MarshalStringToUTF32(string str) {
        if (string.IsNullOrEmpty(str)) {
            IntPtr nullPtr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(nullPtr, 0);
            return nullPtr;
        }

        var codePoints = new System.Collections.Generic.List<int>();
        for (int i = 0; i < str.Length; i++) {
            int codePoint = char.ConvertToUtf32(str, i);
            codePoints.Add(codePoint);
            if (char.IsHighSurrogate(str[i]))
                i++;
        }

        IntPtr ptr = Marshal.AllocHGlobal((codePoints.Count + 1) * 4);
        for (int i = 0; i < codePoints.Count; i++) {
            Marshal.WriteInt32(ptr, i * 4, codePoints[i]);
        }
        Marshal.WriteInt32(ptr, codePoints.Count * 4, 0);
        return ptr;
    }

    static void Main() {
        string test = "/home/doug/test.pck";
        IntPtr ptr = MarshalStringToUTF32(test);

        Console.WriteLine($"Input: {test}");
        Console.WriteLine($"Length: {test.Length}");
        Console.WriteLine("UTF-32 bytes:");

        for (int i = 0; i < (test.Length + 1) * 4; i += 4) {
            int val = Marshal.ReadInt32(ptr, i);
            if (val == 0) {
                Console.WriteLine($"  [{i:D3}] = 0x00000000 (NULL)");
                break;
            }
            Console.WriteLine($"  [{i:D3}] = 0x{val:X8} = {(char)val}");
        }

        Marshal.FreeHGlobal(ptr);
    }
}
