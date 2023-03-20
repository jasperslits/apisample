using JETSample;

bool Override = true;
string Filename = "jasper.xml";
if (! Override)
{

    Filename = "";
}

if (! Override || args.Length != 1)
{
    Console.WriteLine("Provide .xml filename to send or BODID");
}
else
{
    

    if (Filename.Contains("."))
    {
        if (!File.Exists(Filename))
        {
            Console.WriteLine($"File {Filename} does not exit in {Directory.GetCurrentDirectory()}");
        }
        else
        {

            HRXML hrxml = new();
            await hrxml.Runner(Filename);
        }
    }
    else
    {
        int count = args[0].Count(f => f == '-');
        if (count == 4)
        {
            HRXML hrxml = new();
            await hrxml.CheckResults(args[0]);
        }
        else
        {
            Console.WriteLine($"Argument {args[0]} is not a BOD or filename");
        }
    }

  
}


