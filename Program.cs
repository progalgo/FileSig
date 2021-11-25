using System;
using System.IO;

namespace FileSig
{
    class Program
    {
        static void Main(string[] args)
        {
            string filepath = args[1];
            long chunkSize = long.Parse(args[0]);

            ChunkHasher chunkHasher = new ChunkHasher(chunkSize);

            using (var input = File.OpenRead(filepath))
            {
                foreach (ChunkHash item in chunkHasher.QueryHashes(input))
                {
                    Console.WriteLine(item);
                }
            }
        }
    }
}
