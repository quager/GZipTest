using System;
using System.IO;
using System.IO.Compression;
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
                            byte[] header = new byte[8];
                            InputStream.Read(header, 0, header.Length);
                            size = BitConverter.ToInt32(header, 4);
                            if (size == 0)
                            {
                                done = true;
                                break;
                            }

                            InputData[n] = new byte[size];
                            header.CopyTo(InputData[n], 0);
                            InputStream.Read(InputData[n], 8, size - 8);

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
            MemoryStream stream = new MemoryStream(InputData[n]);

            try
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
            catch (Exception ex)
            {
                Display.ShowMessage(ex.ToString());
            }
            finally
            {
                stream.Close();
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
