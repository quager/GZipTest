using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GZipTest
{
    abstract class GZip
    {
        protected const int BlockSize = 10000000;
        protected static int ThreadCount = Environment.ProcessorCount;
        protected byte[][] OutputData = new byte[ThreadCount][];
        protected byte[][] InputData = new byte[ThreadCount][];
        protected object Locker = new object();
        protected object ProgressLocker = new object();
        protected FileStream InputStream = null;
        protected volatile bool Displayed = false;

        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public long CurrentSize { get; protected set; }
        public long FileSize { get; protected set; }

        public GZip(string input, string output)
        {
            InputPath = input;
            OutputPath = output;
        }

        public void Process()
        {
            int blocks = ThreadCount;

            Stopwatch startTime = Stopwatch.StartNew();
            Console.WriteLine("Processing Started.");
            Thread[] Pool = new Thread[ThreadCount];

            for (int n = 0; n < ThreadCount; n++) Pool[n] = null;

            using (FileStream OutputStream = new FileStream(OutputPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                using (InputStream = new FileStream(InputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bool done = false;
                    FileSize = InputStream.Length;

                    while (InputStream.Position < InputStream.Length)
                    {
                        for (int n = 0; n < ThreadCount; n++)
                        {
                            int code = ReadBlock(n);
                            if (code < 0)
                            {
                                done = true;
                                break;
                            }
                            else if (code == 1)
                            {
                                blocks = n;
                                break;
                            }
                            else
                            {
                                Pool[n] = new Thread(ProcessBlock);
                                Pool[n].Start(n);
                            }
                        }

                        if (done) break;

                        for (int n = 0; n < blocks; n++)
                        {
                            if (Pool[n] != null && Pool[n].IsAlive) Pool[n].Join();

                            try
                            {
                                OutputStream.Write(OutputData[n], 0, OutputData[n].Length);
                                OutputData[n] = null;
                            }
                            catch (IOException ex)
                            {
                                Displayed = true;
                                Display.ShowMessage(ex.ToString() + "\r\n\r\nTry Again? (y/n): ", true);
                                Console.WriteLine();
                                Displayed = false;
                                n--;
                            }
                            catch (Exception ex)
                            {
                                Displayed = true;
                                Display.ShowMessage(ex.ToString());
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Processing Completed!");
            startTime.Stop();
            Display.ShowMessage(string.Format("Process Time = {0:F1}s.", startTime.Elapsed.TotalSeconds));
        }

        protected void ProcessBlock(object i)
        {
            int n = (int)i;

            SpecifiedProcessBlock(n);
            lock (ProgressLocker) CurrentSize += InputData[n].Length;
            InputData[n] = null;

            UpdateProgress();
        }

        protected void UpdateProgress()
        {
            lock (ProgressLocker)
            {
                if (Displayed) return;
                string p = string.Format("Progress... {0}%", CurrentSize * 100 / FileSize);
                Console.Write(p + "\r");
            }
        }

        protected abstract int ReadBlock(int n);
        protected abstract void SpecifiedProcessBlock(int n);

        public byte[] StreamToArray(Stream stream)
        {
            stream.Position = 0;
            byte[] array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);
            return array;
        }
    }
}