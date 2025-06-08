using System.IO.Pipes;
using System.Diagnostics;
using System.Text;
using System.Threading;

class ScannerA
{
    static Dictionary<string, Dictionary<string, int>> wordIndex = new();
    static string pipeName = "agent1"; // Pipe for ScannerA and pipe name can be customised 

    static void Main(string[] args)
{
    Console.WriteLine("Scanner A started.");

    if (args.Length == 0)
    {
        Console.WriteLine("Please provide directory path.");
        return;
    }

    if (OperatingSystem.IsWindows())
    {
    // Affinity should be set only when using on windows ( preferably 0 )
    Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0);
    }

    string directoryPath = args[0];

    Thread readThread = new(() => ReadFiles(directoryPath));
    readThread.Start();
    readThread.Join();

    Thread sendThread = new(SendDataToMaster);
sendThread.Start();
sendThread.Join();

    // Original sending logic (disabled for now), can be enabled when we have to run indexing without Master program.
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
        using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.Out);
        pipe.Connect();

        using StreamWriter writer = new(pipe, Encoding.UTF8);

        foreach (var file in wordIndex)
        {
            foreach (var word in file.Value)
            {
                writer.WriteLine($"{file.Key}:{word.Key}:{word.Value}");
            }
        }

        writer.WriteLine("EOF");
    }
}
