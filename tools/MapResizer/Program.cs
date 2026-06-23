using System;
using System.IO;
using System.IO.Compression;

namespace MapResizer
{
    class Program
    {
        const int GZIP_HEADER_OFFSET = 40;
        const int MAX_DIMENSION = 32768;
        const long MAX_AREA = 67108864L;

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                var mapsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Captain of Industry", "Maps");

                Console.WriteLine("COI Map Resizer");
                Console.WriteLine("===============");
                Console.WriteLine();
                Console.WriteLine("Usage: MapResizer <map-file> <width>x<height>");
                Console.WriteLine("       MapResizer <map-file> --info");
                Console.WriteLine();
                Console.WriteLine("  Dimensions are in tiles. Must be multiples of 256.");
                Console.WriteLine($"  Max dimension: {MAX_DIMENSION}, max area: {MAX_AREA:N0} tiles");
                Console.WriteLine($"  Max square: 8192x8192");
                Console.WriteLine();
                Console.WriteLine($"  Maps folder: {mapsDir}");

                if (Directory.Exists(mapsDir))
                {
                    Console.WriteLine();
                    Console.WriteLine("  Available maps:");
                    foreach (var f in Directory.GetFiles(mapsDir, "*.map"))
                        Console.WriteLine($"    {Path.GetFileName(f)}");
                }

                return 1;
            }

            var mapPath = args[0];
            if (!File.Exists(mapPath))
            {
                var mapsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Captain of Industry", "Maps");
                var candidate = Path.Combine(mapsDir, mapPath);
                if (File.Exists(candidate))
                    mapPath = candidate;
                else if (File.Exists(candidate + ".map"))
                    mapPath = candidate + ".map";
                else
                {
                    Console.Error.WriteLine($"File not found: {args[0]}");
                    return 1;
                }
            }

            if (args[1] == "--info")
            {
                ShowInfo(mapPath);
                return 0;
            }

            var parts = args[1].Split('x', 'X');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int newW) || !int.TryParse(parts[1], out int newH))
            {
                Console.Error.WriteLine($"Invalid size format: {args[1]}. Expected WIDTHxHEIGHT (e.g. 8192x8192)");
                return 1;
            }

            if (newW % 256 != 0 || newH % 256 != 0)
            {
                Console.Error.WriteLine($"Dimensions must be multiples of 256. Got {newW}x{newH}");
                return 1;
            }
            if (newW < 256 || newH < 256 || newW > MAX_DIMENSION || newH > MAX_DIMENSION)
            {
                Console.Error.WriteLine($"Dimensions must be 256..{MAX_DIMENSION}. Got {newW}x{newH}");
                return 1;
            }
            if ((long)newW * newH > MAX_AREA)
            {
                Console.Error.WriteLine($"Area {(long)newW * newH:N0} exceeds engine max of {MAX_AREA:N0}");
                return 1;
            }

            return Resize(mapPath, newW, newH);
        }

        static void ShowInfo(string mapPath)
        {
            var raw = File.ReadAllBytes(mapPath);
            var data = Decompress(raw);

            if (TryFindSizePair(data, out int offset, out int w, out int h))
            {
                Console.WriteLine($"Map: {Path.GetFileName(mapPath)}");
                Console.WriteLine($"Size: {w} x {h} tiles");
                Console.WriteLine($"Area: {(long)w * h:N0} tiles ({(long)w * h * 4 / 1_000_000.0:F1} km^2)");
                Console.WriteLine($"Dimensions: {w * 2 / 1000.0:F1} km x {h * 2 / 1000.0:F1} km");
            }
            else
            {
                Console.Error.WriteLine("Could not find map size in file.");
            }
        }

        static int Resize(string mapPath, int newW, int newH)
        {
            var raw = File.ReadAllBytes(mapPath);
            var data = Decompress(raw);

            var allPairs = FindAllSizePairs(data);
            if (allPairs.Count == 0)
            {
                Console.Error.WriteLine("Could not find map size in file.");
                return 1;
            }

            Console.WriteLine($"Current size: {allPairs[0].W} x {allPairs[0].H} ({(long)allPairs[0].W * allPairs[0].H:N0} tiles)");
            Console.WriteLine($"New size:     {newW} x {newH} ({(long)newW * newH:N0} tiles)");
            Console.WriteLine($"Patching {allPairs.Count} size location(s)...");

            var newWBytes = EncodeVarint(ZigZagEncode(newW));
            var newHBytes = EncodeVarint(ZigZagEncode(newH));
            int newPairLen = newWBytes.Length + newHBytes.Length;

            // Patch from last to first so offsets remain valid
            using (var ms = new MemoryStream(data.Length + allPairs.Count * 4))
            {
                int srcPos = 0;
                for (int i = 0; i < allPairs.Count; i++)
                {
                    var pair = allPairs[i];
                    ms.Write(data, srcPos, pair.Offset - srcPos);
                    ms.Write(newWBytes, 0, newWBytes.Length);
                    ms.Write(newHBytes, 0, newHBytes.Length);
                    srcPos = pair.Offset + pair.Length;
                }
                ms.Write(data, srcPos, data.Length - srcPos);
                data = ms.ToArray();
            }

            var backup = mapPath + ".bak";
            if (!File.Exists(backup))
                File.Copy(mapPath, backup);

            var compressed = Compress(data);
            var output = new byte[GZIP_HEADER_OFFSET + compressed.Length];
            Array.Copy(raw, 0, output, 0, GZIP_HEADER_OFFSET);
            Array.Copy(compressed, 0, output, GZIP_HEADER_OFFSET, compressed.Length);

            File.WriteAllBytes(mapPath, output);
            Console.WriteLine($"Saved. Backup at: {Path.GetFileName(backup)}");
            return 0;
        }

        static System.Collections.Generic.List<(int Offset, int Length, int W, int H)> FindAllSizePairs(byte[] data)
        {
            var results = new System.Collections.Generic.List<(int Offset, int Length, int W, int H)>();
            for (int i = 0; i < data.Length - 4; i++)
            {
                var v1 = DecodeVarint(data, i);
                int d1 = ZigZagDecode(v1.Value);
                if (d1 >= 256 && d1 <= MAX_DIMENSION && d1 % 256 == 0)
                {
                    var v2 = DecodeVarint(data, v1.NextOffset);
                    int d2 = ZigZagDecode(v2.Value);
                    if (d2 >= 256 && d2 <= MAX_DIMENSION && d2 % 256 == 0)
                    {
                        int pairLen = v2.NextOffset - i;
                        // Only match if both values equal (same W and H as original map)
                        if (results.Count == 0 || (d1 == results[0].W && d2 == results[0].H))
                        {
                            results.Add((i, pairLen, d1, d2));
                            i = v2.NextOffset - 1;
                        }
                    }
                }
            }
            return results;
        }

        static bool TryFindSizePair(byte[] data, out int offset, out int w, out int h)
        {
            // Strategy: find the map name string, then look backwards for the
            // RelTile2i size pair that precedes it in the serialized preview data.
            // The serialization order is: ..., MapSize (RelTile2i), Name (string), ...

            var nameMarker = System.Text.Encoding.UTF8.GetBytes("New map");
            int nameOffset = -1;
            for (int i = 0; i < data.Length - nameMarker.Length; i++)
            {
                if (data[i] == nameMarker.Length && MatchBytes(data, i + 1, nameMarker))
                {
                    nameOffset = i;
                    break;
                }
            }

            if (nameOffset < 0)
            {
                // Try any map name - scan for size pairs before known strings
                // Fall back to scanning for varint pairs where both decode to 256-aligned values
                for (int i = 0; i < Math.Min(2000, data.Length - 10); i++)
                {
                    var v1 = DecodeVarint(data, i);
                    int d1 = ZigZagDecode(v1.Value);
                    if (d1 >= 256 && d1 <= MAX_DIMENSION && d1 % 256 == 0)
                    {
                        var v2 = DecodeVarint(data, v1.NextOffset);
                        int d2 = ZigZagDecode(v2.Value);
                        if (d2 >= 256 && d2 <= MAX_DIMENSION && d2 % 256 == 0)
                        {
                            offset = i;
                            w = d1;
                            h = d2;
                            return true;
                        }
                    }
                }

                offset = 0; w = 0; h = 0;
                return false;
            }

            // Scan backwards from nameOffset for the size pair
            for (int i = nameOffset - 2; i >= Math.Max(0, nameOffset - 40); i--)
            {
                var v1 = DecodeVarint(data, i);
                int d1 = ZigZagDecode(v1.Value);
                if (d1 >= 256 && d1 <= MAX_DIMENSION && d1 % 256 == 0)
                {
                    var v2 = DecodeVarint(data, v1.NextOffset);
                    int d2 = ZigZagDecode(v2.Value);
                    if (d2 >= 256 && d2 <= MAX_DIMENSION && d2 % 256 == 0 && v2.NextOffset <= nameOffset)
                    {
                        offset = i;
                        w = d1;
                        h = d2;
                        return true;
                    }
                }
            }

            offset = 0; w = 0; h = 0;
            return false;
        }

        static bool MatchBytes(byte[] data, int offset, byte[] pattern)
        {
            if (offset + pattern.Length > data.Length) return false;
            for (int i = 0; i < pattern.Length; i++)
                if (data[offset + i] != pattern[i]) return false;
            return true;
        }

        static byte[] Decompress(byte[] raw)
        {
            using (var input = new MemoryStream(raw, GZIP_HEADER_OFFSET, raw.Length - GZIP_HEADER_OFFSET))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                return output.ToArray();
            }
        }

        static byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gz = new GZipStream(output, CompressionLevel.Optimal))
                    gz.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        static int ZigZagEncode(int value) => (value << 1) ^ (value >> 31);
        static int ZigZagDecode(int value) => (value >> 1) ^ -(value & 1);

        static byte[] EncodeVarint(int value)
        {
            using (var ms = new MemoryStream(5))
            {
                uint v = (uint)value;
                while (v >= 0x80)
                {
                    ms.WriteByte((byte)(v | 0x80));
                    v >>= 7;
                }
                ms.WriteByte((byte)v);
                return ms.ToArray();
            }
        }

        static (int Value, int NextOffset) DecodeVarint(byte[] data, int offset)
        {
            int result = 0, shift = 0;
            while (offset < data.Length)
            {
                byte b = data[offset++];
                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
                if (shift > 28) break;
            }
            return (result, offset);
        }
    }
}
