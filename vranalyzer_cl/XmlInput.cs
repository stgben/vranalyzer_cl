using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text;
using System.Data;

namespace vranalyzer_cl
{
    class XmlInput
    {
        public XElement ReadData(string filePath)
        {
            XElement root = XElement.Load(filePath);

            return root;
        }

        public IEnumerable<XElement> GetChannel(string channelID)
        {
            return null;
        }

        public IEnumerable<XElement> GetStimulusSet(string channelID, string setID)
        {
            return null;
        }

        public IEnumerable<XElement> GetTimeSlice(string channelID, string setID, string timeID)
        {

            return null;
        }

        public double GetAverage(IEnumerable<XElement> set)
        {
            double sum = set.Sum(item => Convert.ToDouble(item.Value));
            double avg = sum / Enumerable.Count(set);

            return avg;
        }

        public string StripFileType(string fileName)
        {
            int lastPeriod = fileName.LastIndexOf(".");
            return fileName.Substring(0, lastPeriod);
        }

        class ChannelAverage
        {
            public List<double> averages { get; set; }
        }
        public XmlInput(string filePath, string fileName)
        {
            /**
             * filePath will be something like "C:\Users\Ben\"
             * fileName will be something like "test.xml"
             **/

            List<double> channelAverages = new List<double>();

            XElement xmlFile = ReadData(filePath + fileName);

            IEnumerable<XElement> channel =
            from el in xmlFile.Elements("Channel")
            where (string)el.Attribute("id") == "21"
            select el;

            
            IEnumerable<XElement> stimSet =
                from el in channel.Elements("StimulusSet")
                select el;

            foreach (int number in Enumerable.Range(0, 10).ToList())
            {
                IEnumerable<XElement> voltSet =
                from voltage in stimSet.Elements("Time")
                where (int)voltage.Attribute("index") % 10 == number
                select voltage;

                IEnumerable<double> chunkValues =
                    from voltage in voltSet
                    select Convert.ToDouble(voltage.Value);

                double avg = GetAverage(voltSet);

                channelAverages.Add(avg);

                Console.WriteLine("Avg = " + avg);

                foreach (double el in chunkValues)
                    Console.WriteLine(el);
                
                Console.WriteLine("------------------\n");
            }

            foreach (double avg in channelAverages)
                Console.WriteLine("channel avgs: " + avg); 

            }
       
        
    }
}
