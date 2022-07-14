if (args.Length == 0)
{
    throw new Exception("Please specify paths using the following format: \"from:to\"");
}

foreach (var arg in args)
{
    var tokens = arg.Split(':');
    if (tokens.Length != 2)
    {
        throw new Exception("Invalid token count!");
    }

    var from = tokens[0];
    var to = tokens[1];

    Console.WriteLine($"Copying {from} to {to}");

    File.Copy(from, to, true);
}
