using System.Text;
using System.IO.Compression;
using System.Security.Cryptography;

namespace codecrafters_git.src
{
    internal class Blob: Object
    {
        private Blob(string hash) : base(hash) { }

        public static Blob Create(FileInfo source)
        {
            using var fs = source.OpenRead();
            using MemoryStream ms = new();
            ms.Write(Encoding.ASCII.GetBytes($"blob {source.Length}\0"));
            fs.CopyTo(ms);
            string hashHex = HashHex(SHA1.HashData(ms.ToArray()));
            ms.Position = 0;
            Blob blob = new(hashHex);
            blob.Write(ms);
            return blob;
        }

        public static Blob Open(string hash)
        {
            return new Blob(hash);
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
            int length = int.Parse(ReadStringUntilByte(br, 0));
            byte[] content = br.ReadBytes(length);
            return content;
        }
    }
}
