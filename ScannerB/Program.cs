using System.IO.Pipes;
using System.Diagnostics;
using System.Text;
using System.Threading;

class ScannerB
{
    static Dictionary<string, Dictionary<string, int>> wordIndex = new();
    static string pipeName = "agent2"; // Pipe for the ScannerB and pipe name can be chnaged ( here it is agent2 )

    static void Main(string[] args)
    {
        Console.WriteLine("Scanner B started.");

        if (args.Length == 0)
        {
            Console.WriteLine("Please provide directory path.");
            return;
        }

        if (OperatingSystem.IsWindows())
    {
    // Affinity should be set only when using on windows ( preferably 1 )
    Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 1);
    }
        string directoryPath = args[0];

        // Same as mentioned in ScannerA ( can be opened when you want to run the program without Master Program)
        // Thread sendThread = new(SendDataToMaster);
        // sendThread.Start();
        // sendThread.Join();
    }

    static void ReadFiles(string directoryPath)
    {
        var files = Directory.GetFiles(directoryPath, "*.txt");

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var words = content.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

            string fileName = Path.GetFileName(file);

            foreach (var word in words)
            {
                var lower = word.ToLower();
                if (!wordIndex.ContainsKey(fileName))
                    wordIndex[fileName] = new Dictionary<string, int>();

                if (!wordIndex[fileName].ContainsKey(lower))
                    wordIndex[fileName][lower] = 0;

                wordIndex[fileName][lower]++;
            }
        }
    }

    static void SendDataToMaster()
    {
        using NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.Out);
        pipeClient.Connect();

        using StreamWriter writer = new(pipeClient, Encoding.UTF8);

        foreach (var file in wordIndex)
        {
            foreach (var word in file.Value)
            {
                writer.WriteLine($"{file.Key}:{word.Key}:{word.Value}");
            }
        }

        writer.WriteLine("EOF"); // End of file transmission signal
    }
}
