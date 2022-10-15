using System;
using System.Collections.Generic;
using System.Linq;

namespace YAMMR.Helpers;

public class Crc32
{
    private const uint SGenerator = 0xEDB88320;
    private readonly uint[] _mChecksumTable;

    public Crc32()
    {
        _mChecksumTable = Enumerable
            .Range(0, 256)
            .Select(i =>
            {
                var tableEntry = (uint)i;
                for (var j = 0; j < 8; ++j)
                    tableEntry =
                        (tableEntry & 1) != 0 ? SGenerator ^ (tableEntry >> 1) : tableEntry >> 1;
                return tableEntry;
            })
            .ToArray();
    }

    public uint Get<T>(IEnumerable<T> byteStream)
    {
        try
        {
            return ~byteStream.Aggregate(
                0xFFFFFFFF,
                (checksumRegister, currentByte) =>
                    _mChecksumTable[(checksumRegister & 0xFF) ^ Convert.ToByte(currentByte)]
                    ^ (checksumRegister >> 8)
            );
        }
        catch (FormatException e)
        {
            throw new Exception("Could not read the stream out as bytes.", e);
        }
        catch (InvalidCastException e)
        {
            throw new Exception("Could not read the stream out as bytes.", e);
        }
        catch (OverflowException e)
        {
            throw new Exception("Could not read the stream out as bytes.", e);
        }
    }
}
