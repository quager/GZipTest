using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GZipTest
{
    abstract class GZip : IDisposable
    {
        protected int BlocksNumber = 1;
        protected static int MaxBlocks = 100;
        protected const int BlockSize = 10000000;
        protected static int ThreadCount = Environment.ProcessorCount;
        protected byte[][] OutputData = null;
        protected byte[][] InputData = null;
        protected Queue<byte[][]> InputQueue = new Queue<byte[][]>();
        protected Queue<byte[][]> OutputQueue = new Queue<byte[][]>();
        protected FileStream InputStream = null;
        protected volatile bool InWork = true;
        protected volatile bool IsReading = true;
        protected volatile bool Displayed = false;
        protected object InLocker = new object();
        protected object OutLocker = new object();
        protected AutoResetEvent[] ProcessingReady = new AutoResetEvent[ThreadCount];
        protected AutoResetEvent[] Processed = new AutoResetEvent[ThreadCount];
        protected AutoResetEvent InputReady = new AutoResetEvent(false);
        protected Semaphore ResetIn = null;
        protected Thread TReader, PoolManager, TWriter;
        protected Stopwatch startTime = null;
        protected int CurrentBlock = 0;

        protected delegate void FatalException(Exception ex);
        protected event FatalException OnFatalException;

        public string InputPath { get; set; }
        public string OutputPath { get; set; }

        public GZip(string input, string output)
        {
            InputPath = input;
            OutputPath = output;
            OnFatalException += GZip_OnFatalException;
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            int blocks = (int)(ramCounter.NextValue() * 200000 / BlockSize / ThreadCount);
            if (blocks < MaxBlocks) MaxBlocks = blocks;
            ResetIn = new Semaphore(MaxBlocks, MaxBlocks);
        }

        public void Dispose()
        {
            InWork = false;
            JoinAll();

            if (TReader.IsAlive)
            {
                ResetIn.Release();
                TReader.Join();
            }

            if (PoolManager.IsAlive)
            {
                InputReady.Set();
                PoolManager.Join();
            }

            if (TWriter.IsAlive) TWriter.Join();
        }

        private void GZip_OnFatalException(Exception ex)
        {
            Dispose();

            if (ex != null) Console.WriteLine(ex.ToString());

            Console.WriteLine("\r\nInterrupted!");
            ProcessCompleted();
        }

        public void Process()
        {
            int blocks = ThreadCount;

            startTime = Stopwatch.StartNew();
            Console.WriteLine("Processing Started.");

            for (int n = 0; n < ThreadCount; n++)
            {
                ProcessingReady[n] = new AutoResetEvent(false);
                Processed[n] = new AutoResetEvent(false);
                new Thread(ProcessBlock).Start(n);
            }
            
            TReader = new Thread(Reader);
            TReader.Start();
            PoolManager = new Thread(PoolManage);
            PoolManager.Start();
            TWriter = new Thread(Writer);
            TWriter.Start();
        }

        protected void Reader()
        {
            byte[][] Block = new byte[ThreadCount][];

            using (InputStream = new FileStream(InputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BlocksNumber = (int)(InputStream.Length / BlockSize) + 1;

                while (InputStream.Position < InputStream.Length)
                {
                    ResetIn.WaitOne();

                    if (!InWork) break;

                    for (int n = 0; n < ThreadCount; n++)
                    {
                        try
                        {
                            Block[n] = ReadBlock(n);
                        }
                        catch (Exception ex)
                        {
                            Displayed = true;
                            OnFatalException?.Invoke(ex);
                        }
                    }

                    lock (InLocker)
                    {
                        InputQueue.Enqueue(Block);
                        Block = new byte[ThreadCount][];
                        InputReady.Set();
                    }
                }
            }

            lock (InLocker)
            {
                InputQueue.Enqueue(null);
                Monitor.PulseAll(InLocker);
                InputReady.Set();
            }

            IsReading = false;
        }

        protected void PoolManage()
        {
            while (InWork)
            {
                if (IsReading) InputReady.WaitOne();

                if (!InWork) break;

                lock (InLocker)
                {
                    while (InputQueue.Count == 0) Monitor.Wait(InLocker);

                    InputData = InputQueue.Dequeue();
                }

                if (InputData == null)
                {
                    lock (OutLocker)
                    {
                        OutputQueue.Enqueue(null);
                        Monitor.PulseAll(OutLocker);
                    }
                    break;
                }

                ResetIn.Release();
                lock (OutLocker) OutputData = new byte[ThreadCount][];

                for (int i = 0; i < ThreadCount; i++)
                    ProcessingReady[i].Set();

                WaitHandle.WaitAll(Processed);

                lock (OutLocker)
                {
                    OutputQueue.Enqueue(OutputData);
                    Monitor.PulseAll(OutLocker);
                }
            }
        }

        protected void Writer()
        {
            byte[][] Block = null;

            using (FileStream OutputStream = new FileStream(OutputPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                while (InWork)
                {
                    lock (OutLocker)
                    {
                        while (OutputQueue.Count == 0) Monitor.Wait(OutLocker);

                        Block = OutputQueue.Dequeue();
                    }

                    if (Block == null)
                    {
                        InWork = false;
                        JoinAll();
                        break;
                    }

                    for (int n = 0; n < ThreadCount; n++)
                    {
                        if (!InWork) return;

                        try
                        {
                            if (Block[n] == null)
                            {
                                InWork = false;
                                JoinAll();
                                break;
                            }

                            OutputStream.Write(Block[n], 0, Block[n].Length);

                            CurrentBlock++;
                            UpdateProgress();
                        }
                        catch (IOException ex)
                        {
                            Displayed = true;
                            if (Display.ShowMessage(ex.ToString() + "\r\n\r\nTry Again? (y/n): ", true))
                            {
                                Displayed = false;
                                n--;
                                continue;
                            }
                            else
                            {
                                OnFatalException?.Invoke(null);
                                return;
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            Displayed = true;
                            OnFatalException?.Invoke(ex);
                            return;
                        }
                    }

                    Block = null;
                }
            }

            Console.WriteLine("Processing Completed!");
            ProcessCompleted();
        }

        protected void ProcessCompleted()
        {
            startTime.Stop();
            Display.ShowMessage(string.Format("Process Time = {0:F1}s.", startTime.Elapsed.TotalSeconds));
        }

        protected void ProcessBlock(object i)
        {
            int n = (int)i;

            try
            {
                while (InWork)
                {
                    ProcessingReady[n].WaitOne();

                    lock (InLocker)
                    {
                        if (InputData == null || InputData[n] == null || !InWork) break;
                    }

                    SpecifiedProcessBlock(n);
                    
                    lock (InLocker) InputData[n] = null;
                    
                    Processed[n].Set();
                }
            }
            catch (Exception ex)
            {
                Displayed = true;
                OnFatalException?.Invoke(ex);
            }
            
            Processed[n].Set();
        }

        protected void UpdateProgress()
        {
            if (Displayed) return;
            string p = string.Format("Progress... {0}%", CurrentBlock * 100 / BlocksNumber);
            Console.Write(p + "\r");
        }

        protected void JoinAll()
        {
            for (int i = 0; i < ThreadCount; i++)
            {
                ProcessingReady[i].Set();
            }
        }

        protected abstract byte[] ReadBlock(int n);
        protected abstract void SpecifiedProcessBlock(int n);

        public static byte[] StreamToArray(Stream stream)
        {
            stream.Position = 0;
            byte[] array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);
            return array;
        }
    }
}