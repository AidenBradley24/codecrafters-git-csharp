using codecrafters_git.src;
using System;
using System.IO;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

string? plumb = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--p")
    {
        plumb = args[++i];
    }
}

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

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
    var blob = new Blob(plumb!);
    Console.WriteLine(blob.Read());
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}