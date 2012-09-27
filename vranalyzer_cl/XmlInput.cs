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

        public string StripFileType(string fileName)
        {
            int lastPeriod = fileName.LastIndexOf(".");
            return fileName.Substring(0, lastPeriod);
        }

        public XmlInput(string filePath, string fileName)
        {
            /**
             * filePath will be something like "C:\Users\Ben\"
             * fileName will be something like "test.xml"
             **/
            XElement xmlFile = ReadData(filePath + fileName);


        }
       
        
    }
}
