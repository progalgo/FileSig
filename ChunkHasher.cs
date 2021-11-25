using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Cryptography;

namespace FileSig
{
    class ChunkHasher
    {
        public long ChunkSize { get; }

        public ChunkHasher(long chunkSize)
        {
            ChunkSize = chunkSize;
        }

        public ParallelQuery<ChunkHash> QueryHashes(FileStream inputStream)
        {
            var chunks = EnumerateChunks(inputStream);
            return QueryHashes(chunks);
        }

        private IEnumerable<(long id, Stream chunk)> EnumerateChunks(FileStream inputStream)
        {
            long length = inputStream.Length;

            var offsets = EnumerateOffsets(length);

            using (MemoryMappedFile file =
                MemoryMappedFile.CreateFromFile(
                    inputStream,
                    null,
                    0,
                    MemoryMappedFileAccess.Read,
                    HandleInheritability.None,
                    false))
            {
                long counter = 0;

                foreach (var offset in offsets)
                {
                    var remainig = length - offset;
                    var chunkSize = Math.Min(remainig, ChunkSize);
                    var mStream = file.CreateViewStream(
                        offset,
                        chunkSize,
                        MemoryMappedFileAccess.Read);

                    yield return (counter, mStream);
                    
                    counter++;
                }
            }
        }

        private IEnumerable<long> EnumerateOffsets(long fileLength)
        {
            long offset = 0;

            while (offset < fileLength)
            {
                yield return offset;
                offset += ChunkSize;
            }
        }

        private ParallelQuery<ChunkHash> QueryHashes(IEnumerable<(long id, Stream buffer)> chunks)
        {
            return chunks
                .AsParallel()
                .Select(ComputeHash);
        }

        private ChunkHash ComputeHash((long id, Stream input) chunk)
        {
            using (var hash = SHA256.Create())
            using (chunk.input)
            {
                return new ChunkHash
                {
                    Order = chunk.id,
                    Hash = hash.ComputeHash(chunk.input)
                };
            }
        }
    }

    struct ChunkHash
    {
        public long Order { get; set; }
        public byte[] Hash { get; set; }

        public override string ToString()
        {
            return $"Chunk: {Order}, hash: {BitConverter.ToString(Hash)}";
        }
    }
}
