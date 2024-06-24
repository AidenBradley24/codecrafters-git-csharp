using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;

namespace codecrafters_git.src
{
    internal class Tree : Object
    {
        record Entry(byte[] Hash, string Name, string Mode);

        private List<Entry> entries;
        private Tree(string hash) : base(hash)
        {
            entries = [];
        }

        public static Tree Create(DirectoryInfo sourceDir)
        {
            List<Entry> entries = [];
            foreach (var dir in sourceDir.EnumerateDirectories())
            {
                if (dir.Name == ".git") continue;
                Tree child = Create(dir);
                entries.Add(new(child.HashBytes, dir.Name, "40000"));
            }

            foreach (var file in sourceDir.EnumerateFiles())
            {
                var blob = Blob.Create(file);
                entries.Add(new(blob.HashBytes, file.Name, "100644"));
            }

            using var ms = new MemoryStream();
            foreach (var entry in entries)
            {
                ms.Write(Encoding.ASCII.GetBytes($"{entry.Mode} {entry.Name}\0"));
                ms.Write(entry.Hash);
            }

            long length = ms.Length;
            ms.Position = 0;
            
            using var ms2 = new MemoryStream();
            ms2.Write(Encoding.ASCII.GetBytes($"tree {length}\0"));
            ms.CopyTo(ms2);
            ms.Dispose();
            string hashHex = HashHex(SHA1.HashData(ms2.ToArray()));

            Tree tree = new(hashHex)
            {
                entries = entries
            };

            ms2.Position = 0;
            tree.Write(ms2);
            return tree;
        }

        public static Tree Open(string hash)
        {
            var tree = new Tree(hash);

            using var fs = tree.Target.OpenRead();
            using var zl = new ZLibStream(fs, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            zl.CopyTo(ms);
            zl.Flush();
            ms.Seek("tree ".Length, SeekOrigin.Begin);
            using var br = new BinaryReader(ms);
            int length = int.Parse(ReadStringUntilByte(br, 0));

            while (length > 0)
            {
                string mode = ReadStringUntilByte(br, 0x20); // until space
                string name = ReadStringUntilByte(br, 0);
                byte[] sha = br.ReadBytes(20);
                length -= (mode.Length + name.Length + sha.Length + 2);
                tree.entries.Add(new Entry(sha, name, mode));
            }

            return tree;
        }

        public string ToString(bool nameOnly)
        {
            StringBuilder sb = new();
            foreach (Entry entry in entries)
            {
                if (!nameOnly)
                {
                    sb.Append(entry.Mode);
                    sb.Append(' ');
                    string type = entry.Mode switch
                    {
                        "040000" or "40000" => "tree",
                        _ => "blob"
                    };
                    sb.Append(type);
                    sb.Append(HashHex(entry.Hash));
                    sb.Append("    ");
                }
                sb.Append(entry.Name);
                sb.Append('\n');
            }
            return sb.ToString();
        }

    }
}
