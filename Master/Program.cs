using System.IO.Pipes;
using System.Diagnostics;
using System.Text;
using System.Threading;

class Master
{
    static Dictionary<string, Dictionary<string, int>> globalIndex = new();
    static object lockObj = new(); // for thread-safe access

    static void Main(string[] args)
    {
        Console.WriteLine("Master started.");

         if (OperatingSystem.IsWindows())
    {
    // Affinity should be set only when using on windows ( preferably 2)
    Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 2);
    }

        // ✅ Validate pipe names and check if the pipe name is same as Scanner A & B 
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Master.exe agent1 agent2");
            return;
        }

        string pipeName1 = args[0];
        string pipeName2 = args[1];

        Thread thread1 = new(() => ListenToPipe(pipeName1));
        Thread thread2 = new(() => ListenToPipe(pipeName2));

        thread1.Start();
        thread2.Start();

        thread1.Join();
        thread2.Join();

        Console.WriteLine("\n📊 Final Word Index:");
        foreach (var file in globalIndex)
        {
            foreach (var word in file.Value)
            {
                Console.WriteLine($"{file.Key}:{word.Key}:{word.Value}");
            }
        }
    }

    static void ListenToPipe(string pipeName)
    {
        Console.WriteLine($"🔌 Listening on pipe: {pipeName}");

        using NamedPipeServerStream pipeServer = new(pipeName, PipeDirection.In);
        pipeServer.WaitForConnection();

        using StreamReader reader = new(pipeServer, Encoding.UTF8);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (line == "EOF") break;

            string[] parts = line.Split(':');
            if (parts.Length != 3) continue;

            string file = parts[0];
            string word = parts[1];
            int count = int.TryParse(parts[2], out int c) ? c : 0;

            lock (lockObj)
            {
                if (!globalIndex.ContainsKey(file))
                    globalIndex[file] = new Dictionary<string, int>();

                if (!globalIndex[file].ContainsKey(word))
                    globalIndex[file][word] = 0;

                globalIndex[file][word] += count;
            }
        }

        Console.WriteLine($"✅ Finished reading from: {pipeName}");
    }
}
