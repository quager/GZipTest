using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    class Compression : GZip
    {
        public Compression(string input, string output) : base(input, output) { }

        protected override byte[] ReadBlock(int n)
        {
            if (InputStream == null) return null;

            byte[] Block = null;

            int size = 0;
            if (InputStream.Length - InputStream.Position >= BlockSize) size = BlockSize;
            else size = (int)(InputStream.Length - InputStream.Position);

            if (size == 0) return null;
                        
            Block = new byte[size];
            InputStream.Read(Block, 0, size);

            return Block;
        }

        protected override void SpecifiedProcessBlock(int n)
        {
            MemoryStream bufstream = new MemoryStream();

            try
            {
                lock (InLocker)
                {
                    using (GZipStream gz = new GZipStream(bufstream, CompressionMode.Compress))
                        gz.Write(InputData[n], 0, InputData[n].Length);
                }

                lock (OutLocker)
                {
                    OutputData[n] = bufstream.ToArray();
                    BitConverter.GetBytes(OutputData[n].Length).CopyTo(OutputData[n], 4);
                }
            }
            finally
            {
                bufstream.Close();
            }
        }
    }
}
