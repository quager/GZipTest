using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    class Compression : GZip
    {
        public Compression(string input, string output) : base(input, output) { }

        protected override int ReadBlock(int n)
        {
            if (InputStream == null) return -1;

            int size = 0;
            if (InputStream.Length - InputStream.Position >= BlockSize) size = BlockSize;
            else size = (int)(InputStream.Length - InputStream.Position);

            if (size == 0) return 1;

            InputData[n] = new byte[size];
            InputStream.Read(InputData[n], 0, size);

            return 0;
        }

        protected override void SpecifiedProcessBlock(int n)
        {
            MemoryStream bufstream = new MemoryStream();

            try
            {
                lock (Locker)
                {
                    using (GZipStream gz = new GZipStream(bufstream, CompressionMode.Compress))
                        gz.Write(InputData[n], 0, InputData[n].Length);

                    OutputData[n] = bufstream.ToArray();
                    BitConverter.GetBytes(OutputData[n].Length).CopyTo(OutputData[n], 4);
                }
            }
            catch (Exception ex)
            {
                Displayed = true;
                Display.ShowMessage(ex.ToString());
            }
            finally
            {
                bufstream.Close();
            }
        }
    }
}
