using System.IO.Compression;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;

namespace codecrafters_git.src
{
    internal class Commit : Object
    {
        public string TreeHash { get => tree; }
        public string Message { get => message; }
        public IEnumerable<string> ParentHashes { get => parents; }
        public User Author { get => author; }
        public User Committer { get => committer; }

        private string message;
        private List<string> parents;
        private User author, committer;
        private string tree;
        public record User(string Name, string Email, DateTime Time);

#pragma warning disable CS8618
        private Commit(string hash) : base(hash) { }
#pragma warning restore CS8618

        public static Commit Create(string treeSha, User author, User committer, string message, IEnumerable<string>? parentHashes)
        {
            using var ms = new MemoryStream();

            ms.Write(Encoding.ASCII.GetBytes("tree "));
            ms.Write(Encoding.ASCII.GetBytes(treeSha));

            if (parentHashes != null)
            {
                foreach (var parent in parentHashes)
                {
                    Console.WriteLine(parent);
                    ms.Write(Encoding.ASCII.GetBytes("parent "));
                    ms.Write(Encoding.ASCII.GetBytes(parent));
                }
            }

            WriteUser("author", author, ms);
            WriteUser("committer", committer, ms);
            ms.Write(Encoding.ASCII.GetBytes(message));

            long length = ms.Length;
            ms.Position = 0;

            using var ms2 = new MemoryStream();
            ms2.Write(Encoding.ASCII.GetBytes($"commit {length}\0"));
            ms.CopyTo(ms2);
            ms.Dispose();
            string hashHex = HashHex(SHA1.HashData(ms2.ToArray()));

            Commit commit = new(hashHex)
            {
                tree = treeSha,
                message = message,
                author = author,
                committer = committer,
                parents = (parentHashes ?? []).ToList()
            };

            ms2.Position = 0;
            commit.Write(ms2);
            return commit;
        }

        public static Commit Open(string hash)
        {
            var commit = new Commit(hash);

            using var fs = commit.Target.OpenRead();
            using var zl = new ZLibStream(fs, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            zl.CopyTo(ms);
            zl.Flush();
            ms.Seek("commit ".Length, SeekOrigin.Begin);
            using var br = new BinaryReader(ms);
            int length = int.Parse(ReadStringUntilByte(br, 0));

            ms.Seek("tree ".Length, SeekOrigin.Current);
            commit.tree = Encoding.ASCII.GetString(br.ReadBytes(20));
            length -= ("tree ".Length + commit.tree.Length);

            commit.parents = [];
            while (true)
            {
                string mode = ReadStringUntilByte(br, 0x20); // until space
                if (mode == "parent")
                {
                    byte[] sha = br.ReadBytes(20);
                    length -= (sha.Length + "parent ".Length);
                    commit.parents.Add(HashHex(sha));
                }
                else
                {
                    // now onto author
                    break;
                }
            }

            length -= "author ".Length;
            commit.author = ParseUser(br, ref length);
            ms.Seek("committer ".Length, SeekOrigin.Current);
            length -= "committer ".Length;
            commit.committer = ParseUser(br, ref length);

            commit.message = Encoding.ASCII.GetString(br.ReadBytes(length));

            return new Commit(hash);
        }

        private static User ParseUser(BinaryReader br, ref int length)
        {
            string name = ReadStringUntilByte(br, 0x3C).TrimEnd(); // until < (trimming the last space)
            length -= (name.Length + 2);
            string email = ReadStringUntilByte(br, 0x3E); // until >
            br.BaseStream.Position++; // skip space
            length -= (email.Length + 2);
            string timeString = ReadStringUntilByte(br, 0x20); // until space
            length -= (timeString.Length + 1);
            long unixTime = long.Parse(timeString);
            DateTime timeUtc = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            string timezoneString = Encoding.ASCII.GetString(br.ReadBytes(5));
            length -= timeString.Length;
            TimeSpan offset = TimeSpan.ParseExact(timezoneString, "hhmm", CultureInfo.InvariantCulture);
            DateTime time = timeUtc + offset;
            return new User(name, email, time);
        }

        private static void WriteUser(string header, User user, Stream stream)
        {
            StringBuilder sb = new(header);
            sb.Append(' ');
            sb.Append(user.Name);
            sb.Append(" <");
            sb.Append(user.Email);
            sb.Append("> ");
            
            long unixTime = ((DateTimeOffset)user.Time).ToUnixTimeSeconds();
            TimeSpan offset = user.Time - user.Time.ToUniversalTime();
            string timeZoneString = offset.ToString("hhmm");
            sb.Append(unixTime);
            sb.Append(' ');
            sb.Append(timeZoneString);
            
            stream.Write(Encoding.ASCII.GetBytes(sb.ToString()));
        }
    }
}
