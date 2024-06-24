using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_git.src
{
    internal class Tree : Object
    {
        record Entry(byte[] Hash, string Name, string Mode);

        private readonly List<Entry> entries;
        private Tree(string hash) : base(hash)
        {
            entries = [];
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
                        "040000" => "tree",
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
