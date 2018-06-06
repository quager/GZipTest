using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Display d = new Display();
            if (args.Length < 2) d.ShowMessage("Not All Parameters are Specified!");

            string Mode = "compress";

            if (args[0].ToLower().Equals("decompress")) Mode = "decompress";
            else if (!args[0].ToLower().Equals("compress")) d.ShowMessage("Operation Not Specified!");

            char[] wrong = { '/', ':', '?', '*', '<', '>', '|', '"' };
            string SrcFileName = args[1];
            if (Path.GetFileName(SrcFileName).IndexOfAny(wrong) >= 0) d.ShowMessage("Wrong Source File Path!");
            if (!File.Exists(SrcFileName)) d.ShowMessage("Source File Not Exists!");
            string DstFileName = args[2];
            if (Path.GetFileName(DstFileName).IndexOfAny(wrong) >= 0) d.ShowMessage("Wrong Destination File Path!");
            if (File.Exists(DstFileName))
            {
                d.ShowMessage("Destination File Exists!\r\nDo You Want to Overwrite it? (y/n): ", true);
                File.Delete(DstFileName);
            }

            GZip gz = new GZip(SrcFileName, DstFileName, Mode);
            gz.OnShowMessage += d.ShowMessage;
            gz.Process();
        }
    }
}
