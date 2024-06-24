using codecrafters_git.src;
using System.Text;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

string? p = null;
string? w = null;
bool nameOnly = false;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-p")
    {
        p = args[++i];
    }
    else if (args[i] == "-w")
    {
        w = args[++i];
    }
    else if (args[i] == "--name-only")
    {
        nameOnly = true;
    }
}

string command = args[0];

if (command == "init")
{
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
    Console.WriteLine("Initialized git directory");
}
else if (command == "cat-file")
{
    if (p == null) throw new Exception("no plumb arg!");
    var blob = Blob.Open(p);
    Console.Write(Encoding.ASCII.GetString(blob.Read()));
}
else if (command == "hash-object")
{
    if (w == null) throw new Exception("no plumb arg!");
    var blob = Blob.Create(new FileInfo(w));
    Console.Write(blob.Hash);
}
else if (command == "ls-tree")
{
    string treeSha = args[^1];
    var tree = Tree.Open(treeSha);
    Console.Write(tree.ToString(nameOnly));
}
else if (command == "write-tree")
{
    var tree = Tree.Create(Directory.CreateDirectory("."));
    Console.Write(tree.Hash);
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}