using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    abstract class GZip
    {
        protected const int BlockSize = 1000000;
        protected static readonly int ThreadCount = Environment.ProcessorCount;
        protected byte[][] OutputData = new byte[ThreadCount][];
        protected byte[][] InputData = new byte[ThreadCount][];
        protected object Locker = new object();

        public string Input { get; set; }
        public string Output { get; set; }
        public long CurrentSize { get; protected set; }
        public long FileSize { get; protected set; }

        public GZip(string input, string output)
        {
            Input = input;
            Output = output;
        }

        public abstract void Process();

        public byte[] StreamToArray(Stream stream)
        {
            stream.Position = 0;
            byte[] array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);
            return array;
        }
    }
}