using System;

namespace GZipTest
{
    static class Display
    {
        public static bool ShowMessage(string message, bool ask = false)
        {
            if (ask)
            {
                Console.Write(message);
                char answer = char.ToLower(Console.ReadKey().KeyChar);
                Console.WriteLine();
                if (answer == 'y') return true;
                else return false;
            }
            else Console.WriteLine(message);
            Console.WriteLine("\r\nPress Any Key to Exit...");
            Console.ReadKey();
            return true;
        }
    }
}