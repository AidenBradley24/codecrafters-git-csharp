using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Security.Cryptography;

namespace codecrafters_git.src
{
    internal class Blob
    {
        public string Hash { get; }
        private FileInfo Target { get => new(Path.Combine(".git", "objects", Hash[..2], Hash[2..])); }

        private Blob(string hash)
        {
            Hash = hash;
        }

        public static Blob Create(FileInfo source)
        {
            using var fs = source.OpenRead();
            using MemoryStream ms = new();
            fs.CopyTo(ms);
            string hashHex = BitConverter.ToString(SHA1.HashData(ms.ToArray()));
            ms.Position = 0;
            Blob blob = new(hashHex);
            blob.Write(ms);
            return blob;
        }

        public static Blob Open(string hash)
        {
            return new Blob(hash);
        }

        private void Write(Stream source)
        {
            using var fs = Target.OpenWrite();
            using var zl = new ZLibStream(fs, CompressionMode.Compress);
            source.CopyTo(zl);
        }

        public byte[] Read()
        {
            using var fs = Target.OpenRead();
            using var zl = new ZLibStream(fs, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            zl.CopyTo(ms);
            zl.Flush();
            ms.Seek("blob ".Length, SeekOrigin.Begin);
            using var br = new BinaryReader(ms);
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
