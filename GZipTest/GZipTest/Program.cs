﻿using System.IO;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2) Display.ShowMessage("Not All Parameters are Specified!");

            string Mode = "compress";

            if (args[0].ToLower().Equals("decompress")) Mode = "decompress";
            else if (!args[0].ToLower().Equals("compress")) Display.ShowMessage("Operation Not Specified!");

            char[] wrong = { '/', ':', '?', '*', '<', '>', '|', '"' };

            string SrcFileName = args[1];

            if (Path.GetFileName(SrcFileName).IndexOfAny(wrong) >= 0)
                Display.ShowMessage("Wrong Source File Path!");

            if (!File.Exists(SrcFileName))
                Display.ShowMessage("Source File Not Exists!");

            string DstFileName = args[2];

            if (!Directory.Exists(Path.GetDirectoryName(DstFileName)) || Path.GetFileName(DstFileName).IndexOfAny(wrong) >= 0)
                Display.ShowMessage("Wrong Destination File Path!");

            if (File.Exists(DstFileName))
            {
                Display.ShowMessage("Destination File Exists!\r\nDo You Want to Overwrite it? (y/n): ", true);
                File.Delete(DstFileName);
            }

            GZip gz = null;
            if (Mode.Equals("compress")) gz = new Compression(SrcFileName, DstFileName);
            else gz = new Decompression(SrcFileName, DstFileName);
            gz.Process();
        }
    }
}
