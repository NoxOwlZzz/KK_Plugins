using KK_Plugins;

internal static class CRC64RegressionTests
{
    internal static long NumericAllocatedBytes { get; private set; }
    internal static long CheckValueAllocatedBytes { get; private set; }
    internal static long LegacyNumericAllocatedBytes { get; private set; }

    internal static void Run()
    {
        GoldenValuesRemainStable();
        NumericAndByteContractsMatchTheCheckpoint();
        InvalidInputBehaviorRemainsStable();
        NumericPathDoesNotAllocate();
        Console.WriteLine(
            $"CRC64 differential tests passed; legacy numeric allocations={LegacyNumericAllocatedBytes} B/10000, " +
            $"optimized numeric allocations={NumericAllocatedBytes} B/10000, " +
            $"check-value allocations={CheckValueAllocatedBytes} B/10000.");
    }

    private static void GoldenValuesRemainStable()
    {
        Equal(0x0000000000000000UL, CRC64Calculator.CalculateCRC64(Array.Empty<byte>()), "empty");
        Equal(0xCFBA63E5EDFAD2C2UL, CRC64Calculator.CalculateCRC64(Array.Empty<byte>(), hashLen: true), "empty with length");
        Equal(0x3D09CFD5562E617CUL, CRC64Calculator.CalculateCRC64(new byte[] { 0 }), "single zero");
        Equal(0x579621102DCFB9A5UL, CRC64Calculator.CalculateCRC64(new byte[] { 0 }, 2048, 512, true), "single zero with length");

        var digits = System.Text.Encoding.ASCII.GetBytes("123456789");
        Equal(0xC8464E40773C4AABUL, CRC64Calculator.CalculateCRC64(digits), "digits");
        Equal(0x61EEE70BE925D3A4UL, CRC64Calculator.CalculateCRC64(digits, 2048, 512, true), "digits with length");

        var range = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        Equal(0x96597E7E54599B3AUL, CRC64Calculator.CalculateCRC64(range, 2048, 512, true), "range production shape");
        Equal(0x0000000000000000UL, CRC64Calculator.CalculateCRC64(range, 0, 0, false), "range no bytes");
        Equal(0x56CCDF6A78FB4D71UL, CRC64Calculator.CalculateCRC64(range, 1, 1, false), "range edges");
        Equal(0x74A71E544E2766A8UL, CRC64Calculator.CalculateCRC64(range, 256, 0, false), "range forward");
        Equal(0x82ADF39C3472BF61UL, CRC64Calculator.CalculateCRC64(range, 0, 256, false), "range reverse");
        Equal(0x6C612E2DA325F8B4UL, CRC64Calculator.CalculateCRC64(range, -1, -2, true), "negative sizes");
    }

    private static void NumericAndByteContractsMatchTheCheckpoint()
    {
        var random = new Random(0x4D455F43);
        foreach (var length in new[] { 0, 1, 2, 511, 512, 513, 2047, 2048, 2049, 4096 })
        {
            var data = new byte[length];
            random.NextBytes(data);
            var sizes = new int?[]
            {
                null,
                -3,
                0,
                1,
                length / 2,
                length,
                length + 7
            };

            foreach (var size in sizes)
            foreach (var sizeEnd in sizes)
            foreach (var hashLength in new[] { false, true })
            {
                var expected = LegacyCalculateCRC64(data, size, sizeEnd, hashLength);
                var actual = CRC64Calculator.CalculateCRC64(data, size, sizeEnd, hashLength);
                Equal(expected, actual, $"numeric length={length}, start={size}, end={sizeEnd}, hashLen={hashLength}");
            }

            foreach (var size in new[] { -3, 0, Math.Min(1, length), length / 2, length })
            foreach (var sizeEnd in new[] { -2, 0, Math.Min(1, length), length / 2, length })
            foreach (var hashLength in new[] { false, true })
            {
                var expected = LegacyCalculateCheckValue(data, size, sizeEnd, hashLength);
                var actual = CRC64Calculator.CalculateCheckValue(data, size, sizeEnd, hashLength);
                BytesEqual(expected, actual, $"bytes length={length}, start={size}, end={sizeEnd}, hashLen={hashLength}");
            }
        }
    }

    private static void InvalidInputBehaviorRemainsStable()
    {
        Equal<byte[]>(null, CRC64Calculator.CalculateCheckValue(null, 1, 1, true), "null check value");
        Throws<NullReferenceException>(
            () => CRC64Calculator.CalculateCRC64(null),
            "null numeric input");
        Throws<IndexOutOfRangeException>(
            () => CRC64Calculator.CalculateCheckValue(new byte[] { 1 }, 2, 0, false),
            "oversized start");
        Throws<IndexOutOfRangeException>(
            () => CRC64Calculator.CalculateCheckValue(new byte[] { 1 }, 0, 2, false),
            "oversized end");
    }

    private static void NumericPathDoesNotAllocate()
    {
        var data = new byte[4096];
        new Random(0x43524336).NextBytes(data);
        ulong sink = 0;
        for (var index = 0; index < 2000; index++)
        {
            sink ^= CRC64Calculator.CalculateCRC64(data, 2048, 512, true);
            sink ^= LegacyCalculateCRC64(data, 2048, 512, true);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10000; index++)
            sink ^= LegacyCalculateCRC64(data, 2048, 512, true);
        LegacyNumericAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10000; index++)
            sink ^= CRC64Calculator.CalculateCRC64(data, 2048, 512, true);
        NumericAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        if (NumericAllocatedBytes != 0)
            throw new InvalidOperationException(
                $"CRC64 numeric path allocated {NumericAllocatedBytes} bytes after warmup.");

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10000; index++)
            sink ^= BitConverter.ToUInt64(
                CRC64Calculator.CalculateCheckValue(data, 2048, 512, true),
                0);
        CheckValueAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        if (CheckValueAllocatedBytes <= NumericAllocatedBytes)
            throw new InvalidOperationException(
                "CRC64 check-value path must retain its contractual byte[8] allocation.");

        GC.KeepAlive(sink);
    }

    private static ulong LegacyCalculateCRC64(
        byte[] data,
        int? size = null,
        int? sizeEnd = null,
        bool hashLen = false)
    {
        size = Math.Min(data.Length, size ?? data.Length);
        sizeEnd = Math.Min(data.Length, sizeEnd ?? data.Length);
        var checkValue = LegacyCalculateCheckValue(data, size.Value, sizeEnd.Value, hashLen);
        Array.Resize(ref checkValue, 8);
        return BitConverter.ToUInt64(checkValue, 0);
    }

    private static byte[] LegacyCalculateCheckValue(
        byte[] data,
        int size,
        int sizeEnd,
        bool hashLen)
    {
        if (data == null)
            return null;

        var crc = ulong.MaxValue;
        int index;
        for (index = 0; index < size; index++)
            crc = LegacyTable[((crc >> 56) ^ data[index]) & 0xFF] ^ (crc << 8);

        var length = data.Length;
        var downEnd = length - sizeEnd - 1;
        for (index = length - 1; index > downEnd; index--)
            crc = LegacyTable[((crc >> 56) ^ data[index]) & 0xFF] ^ (crc << 8);

        if (hashLen)
            foreach (var value in BitConverter.GetBytes(length))
                crc = LegacyTable[((crc >> 56) ^ value) & 0xFF] ^ (crc << 8);

        return BitConverter.GetBytes(crc ^ ulong.MaxValue).Take(8).ToArray();
    }

    private static readonly ulong[] LegacyTable = GenerateLegacyTable();

    private static ulong[] GenerateLegacyTable()
    {
        const ulong polynomial = 0xE4C11DB7B4AC89F3UL;
        const ulong topBit = 1UL << 63;
        var table = new ulong[256];
        for (var index = 0; index < table.Length; index++)
        {
            var remainder = (ulong)(byte)index << 56;
            for (var bit = 0; bit < 8; bit++)
                remainder = (remainder & topBit) != 0
                    ? (remainder << 1) ^ polynomial
                    : remainder << 1;
            table[index] = remainder;
        }
        return table;
    }

    private static void BytesEqual(byte[] expected, byte[] actual, string name)
    {
        if (actual == null || !expected.SequenceEqual(actual))
            throw new InvalidOperationException(name + ": byte arrays differ.");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
    }

    private static void Throws<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }
}
