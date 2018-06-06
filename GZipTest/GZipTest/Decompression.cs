using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    class Decompression : GZip
    {
        public Decompression(string input, string output) : base(input, output) { }

        public override void Process()
        {
            System.Diagnostics.Stopwatch startTime = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine("Decompression Started.");
            Thread[] Pool = new Thread[ThreadCount];

            for (int n = 0; n < ThreadCount; n++) Pool[n] = null;

            using (FileStream OutputStream = new FileStream(Output, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (FileStream InputStream = new FileStream(Input, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bool done = false;
                    int size = 0;
                    FileSize = InputStream.Length;

                    while (InputStream.Position < InputStream.Length)
                    {
                        if (done) break;

                        for (int n = 0; n < ThreadCount; n++)
                        {
                            if (InputStream.Length - InputStream.Position >= BlockSize) size = BlockSize;
                            else size = (int)(InputStream.Length - InputStream.Position);

                            if (size == 0)
                            {
                                done = true;
                                break;
                            }

                            InputData[n] = new byte[size];
                            InputStream.Read(InputData[n], 0, size);

                            Pool[n] = new Thread(DecompressBlock);
                            Pool[n].Start(n);
                        }

                        for (int n = 0; n < ThreadCount; n++)
                        {
                            if (Pool[n] == null) break;

                            Pool[n].Join();
                        }

                        for (int n = 0; n < ThreadCount; n++)
                        {
                            if (Pool[n] == null) break;
                            Pool[n] = null;

                            try
                            {
                                OutputStream.Write(OutputData[n], 0, OutputData[n].Length);
                            }
                            catch (Exception ex)
                            {
                                Display.ShowMessage(ex.ToString());
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Decompression Completed!");
            startTime.Stop();
            Display.ShowMessage(string.Format("Process Time = {0:F2}s.", startTime.Elapsed.TotalSeconds));
        }

        public void DecompressBlock(object i)
        {
            int n = (int)i;
            MemoryStream bufstream = new MemoryStream();
            MemoryStream stream = new MemoryStream(InputData[n]);

            try
            {
                using (GZipStream gz = new GZipStream(bufstream, CompressionMode.Decompress))
                {
                    gz.Write(InputData[n], 0, InputData[n].Length);
                }
                //stream.CopyTo(gz);

                OutputData[n] = bufstream.ToArray();
                BitConverter.GetBytes(OutputData[n].Length).CopyTo(OutputData[n], OutputData[n].Length - 5);
                //byte[] len = BitConverter.GetBytes(OutputData[n].Length);

                //    using (MemoryStream s = new MemoryStream(len.Length + OutputData[n].Length))
                //    {
                //        s.Write(len, 0, len.Length);
                //        s.Write(OutputData[n], 0, OutputData[n].Length);
                //        s.Position = 0;
                //        OutputData[n] = new byte[s.Length];
                //        s.Read(OutputData[n], 0, OutputData[n].Length);
                //    }
            }
            catch (Exception ex)
            {
                Display.ShowMessage(ex.ToString());
            }
            finally
            {
                stream.Close();
                bufstream.Close();
            }

            if (FileSize == 0) return;

            string p;

            lock (Locker)
            {
                CurrentSize += InputData[n].Length;
                p = string.Format("Progress... {0}%", CurrentSize * 100 / FileSize);
            }

            Console.Write(p + "\r");
        }
    }
}
