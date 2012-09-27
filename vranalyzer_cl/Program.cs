using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace vranalyzer_cl
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlOutput.TextToXml(@"C:\Users\Public\Documents\Ishida Lab\", @"control2.dat");
            //XmlInput xi = new XmlInput(@"C:\Users\Public\Documents\Ishida Lab\", @"control2.xml");

            Console.ReadLine();
        }
    }
}
