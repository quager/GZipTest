using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    class Decompression : GZip
    {
        public Decompression(string input, string output) : base(input, output) { }
                
        protected override int ReadBlock(int n)
        {
            if (InputStream == null) return -1;

            int size = 0;
            byte[] header = new byte[8];
            InputStream.Read(header, 0, header.Length);
            size = BitConverter.ToInt32(header, 4);
            if (size == 0) return 1;

            InputData[n] = new byte[size];
            header.CopyTo(InputData[n], 0);
            InputStream.Read(InputData[n], 8, size - 8);

            return 0;
        }

        protected override void SpecifiedProcessBlock(int n)
        {
            MemoryStream stream = new MemoryStream(InputData[n]);

            try
            {
                lock (Locker)
                {
                    using (GZipStream gz = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        int count = 0;
                        byte[] buf = new byte[4096];

                        using (MemoryStream s = new MemoryStream())
                        {
                            while ((count = gz.Read(buf, 0, buf.Length)) > 0)
                            {
                                s.Write(buf, 0, count);
                            }

                            OutputData[n] = StreamToArray(s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Display.ShowMessage(ex.ToString());
            }
            finally
            {
                stream.Close();
            }
        }
    }
}
