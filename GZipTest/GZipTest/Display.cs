using System;

namespace GZipTest
{
    static class Display
    {
        public static void ShowMessage(string message, bool ask = false)
        {
            if (ask)
            {
                Console.Write(message);
                char answer = char.ToLower(Console.ReadKey().KeyChar);
                Console.WriteLine();
                if (answer == 'y') return;
                else Environment.Exit(0);
            }
            else Console.WriteLine(message);
            Console.WriteLine("\r\nPress Any Key to Exit...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}