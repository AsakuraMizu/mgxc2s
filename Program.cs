using Margrete2S.Parser;

if (args.Length > 0)
{
    string path = args[0];
    MgxcParser parser = new(File.ReadAllText(path), 0);
    string target = Path.ChangeExtension(path, "c2s");
    File.WriteAllText(target, parser.ToString());
    Console.WriteLine($"Saving to {target}");
    return 0;
}
else
{
    Console.WriteLine("Usage: mgxc2s file.mgxc");
    return -1;
}
