using System.IO.Compression;
using System.Text;

namespace codecrafters_git.src
{
    internal abstract class Object(string hash)
    {
        public string Hash { get; } = hash;
        public byte[] HashBytes { get => Convert.FromHexString(Hash); }
        protected FileInfo Target { get => new(Path.Combine(".git", "objects", Hash[..2], Hash[2..])); }

        protected static string ReadStringUntilByte(BinaryReader br, byte check)
        {
            List<byte> bees = [];
            while (true)
            {
                byte b = br.ReadByte();
                if (b == check)
                {
                    break;
                }
                bees.Add(b);
            }

            return Encoding.ASCII.GetString(bees.ToArray());
        }

        protected static string HashHex(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        protected void Write(Stream source)
        {
            if (!Target.Directory!.Exists)
            {
                Target.Directory!.Create();
            }
            using var fs = Target.OpenWrite();
            using var zl = new ZLibStream(fs, CompressionMode.Compress);
            source.CopyTo(zl);
        }
    }
}
