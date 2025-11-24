using System;
using System.Runtime.InteropServices;
using System.Linq;

class Test {
    static IntPtr MarshalStringToUTF32(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            IntPtr nullPtr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(nullPtr, 0);
            return nullPtr;
        }

        // Convert string to UTF-32 code points
        var codePoints = new System.Collections.Generic.List<int>();
        for (int i = 0; i < str.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(str, i);
            codePoints.Add(codePoint);

            // Skip the low surrogate if we consumed a surrogate pair
            if (char.IsHighSurrogate(str[i]))
                i++;
        }

        // Allocate memory for UTF-32 string (4 bytes per character + 4 for null terminator)
        IntPtr ptr = Marshal.AllocHGlobal((codePoints.Count + 1) * 4);

        // Write each code point as 4-byte integer
        for (int i = 0; i < codePoints.Count; i++)
        {
            Marshal.WriteInt32(ptr, i * 4, codePoints[i]);
        }

        // Write null terminator
        Marshal.WriteInt32(ptr, codePoints.Count * 4, 0);

        return ptr;
    }

    static void Main() {
        string test = "/home/doug/Desktop/configs.pck";
        IntPtr ptr = MarshalStringToUTF32(test);

        Console.WriteLine($"Original string: {test}");
        Console.WriteLine($"String length: {test.Length}");
        Console.WriteLine($"Pointer: 0x{ptr:X}");
        Console.WriteLine("UTF-32 bytes:");

        for (int i = 0; i < test.Length; i++) {
            int val = Marshal.ReadInt32(ptr, i * 4);
            Console.WriteLine($"  [{i}] = 0x{val:X8} ({val}) = '{(char)val}'");
        }

        int nullTerm = Marshal.ReadInt32(ptr, test.Length * 4);
        Console.WriteLine($"  [null] = 0x{nullTerm:X8}");

        Marshal.FreeHGlobal(ptr);
    }
}
