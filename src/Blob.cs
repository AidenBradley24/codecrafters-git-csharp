using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace codecrafters_git.src
{
    internal class Blob(string hash)
    {
        private readonly string hash = hash;

        public byte[] Read()
        {
            FileInfo info = new(Path.Combine(".git", "objects", hash[..2], hash[2..]));
            using var fs = info.Open(FileMode.Open);
            using var zl = new ZLibStream(fs, CompressionMode.Decompress);
            zl.Seek("blob ".Length, SeekOrigin.Begin);
            using var br = new BinaryReader(zl);
            List<byte> bees = [];
            while (true)
            {
                byte b = br.ReadByte();
                if (b == '\0')
                {
                    break;
                }
                bees.Add(b);
            }

            int length = int.Parse(bees.ToArray());
            byte[] content = br.ReadBytes(length);
            return content;
        }
    }
}
