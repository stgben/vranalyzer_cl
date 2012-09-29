using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

/**
 * --- DataElement: Holds the raw voltage recording that this entire table is made of.
 *          * The index field describes the element's position within a column. The first element
 *          has an index equal to 0, and the final element has an index equal to the number of rows.
 *          * The value field is the voltage recording itself. Pretty self explanatory
 * ---  Column: Describes a single column of elements
 *          * The index indicates which column number we are looking at. Time column excluded.
 *          * colElements is a list of all of the DataElements found within the column.
 *          
 * ---  TextToXml: 
 *          * The general idea here is to take in a text file with a specific format, and output
 *          that information into a more versatile and easier format to work with: XML.
 *          
 *          
 **/
namespace vranalyzer_cl
{

    class XmlOutput
    {
        class DataElement
        {
            public int index { get; set; }
            public double value { get; set; }
        }

        class Column
        {
            public List<DataElement> colElements { get; set; }
            public int index { get; set; }
        }

        class StimulusSet
        {
            public int index { get; set; }
            public int size { get; set; }
        }

        private static List<string> GetChannelNames(StreamReader reader)
        {
            List<string> channelNames = new List<string>();
            foreach (string name in reader.ReadLine().Split('\t'))
            {
                if (name == "T[s]")
                    continue;
                channelNames.Add(name);
            }

            return channelNames;
        }

        private static List<Column> InitializeColDataSet(int numColumns)
        {
            List<Column> colData = new List<Column>();

            for (int i = 0; i < numColumns; i++)
            {
                Column column = new Column();
                column.index = i;
                column.colElements = new List<DataElement>();

                colData.Add(column);
            }
            return colData;
        }

        private static List<string> ReadRows(StreamReader sReader, List<double> timeSlices)
        {
            // read in the row
            string row = sReader.ReadLine();

            // if any rows after the headline row is empty, skip it
            if (row == string.Empty)
                return null;

            // find where the time value ends and data entries begin
            int indexOfFirstTab = row.IndexOf('\t');

            // store the time, from front of row
            double time = Convert.ToDouble(row.Substring(0, indexOfFirstTab));
            timeSlices.Add(time);

            // All of the row's data go into rowValues. 
            List<string> rowValues = new List<string>();

            // Just need to separate the different values from the string cluster
            // holding all of the data
            foreach (string reading in row.Substring(indexOfFirstTab + 1).Split('\t'))
            {
                rowValues.Add(reading);
            }

            return rowValues;
        }

        private static List<Column> RowToColumn(List<Column> colData, int columnCount, int rowIndex, List<string> rowValues)
        {
            for (int i = 0; i < columnCount; i++)
            {
                colData[i].colElements.Add(new DataElement());
                colData[i].colElements[rowIndex].value = Convert.ToDouble(rowValues[i]);
            }

            return colData;
        }

        public static void TextToXml(string inDirectory, string inFile)
        {
            List<string> channelNames = new List<string>();
            List<double> timeSlices = new List<double>();
            List<Column> colDataSet = new List<Column>();
            StreamReader sReader = new StreamReader(inDirectory + inFile);

            channelNames = GetChannelNames(sReader);

            colDataSet = InitializeColDataSet(channelNames.Count);
            

            #region Read File. Store data in columns.

            int rowIndex = 0;
            int totalRows;
            while (!sReader.EndOfStream)
            {
                List<string> rowValues = ReadRows(sReader, timeSlices);

                if (rowValues == null)
                    continue;

                colDataSet = RowToColumn(colDataSet, channelNames.Count, rowIndex, rowValues);
                    
                rowIndex++;
             }
  
             totalRows = rowIndex;
             #endregion


            #region Write Xml Data

           
            XmlDocument doc = new XmlDocument();
            XmlNode declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlNode fileNameNode = doc.CreateElement("File_Name");
            XmlAttribute fileNameAttribute = doc.CreateAttribute("value");
            fileNameAttribute.Value = inFile;
            fileNameNode.Attributes.Append(fileNameAttribute);
            doc.AppendChild(fileNameNode);

            
            int channelIndex = 0;
            int rowDeficit = 10 - (rowIndex % 10);

            foreach (Column column in colDataSet)
            {
                int timeSliceIndex = 0;
                StimulusSet stimulusSet = new StimulusSet();
                stimulusSet.index = 0;
                stimulusSet.size = 0;

                // channel node
                XmlNode channelNode = doc.CreateElement("Channel");
                XmlAttribute channelAttribute = doc.CreateAttribute("id");

                // We only want the number preceeding the "-Min[uV]" portion of the name
                // We will end up with channel id's like "21" , "52" , etc.
                int indexOfDash = channelNames[channelIndex].IndexOf("-");
                string channelName = channelNames[channelIndex].Substring(0, indexOfDash);


                channelAttribute.Value = channelName;
                channelNode.Attributes.Append(channelAttribute);
                fileNameNode.AppendChild(channelNode);

                /**
                 * For each column, we clump every 10 elements together in what is called
                 * a stimulus set. Each stimulus set consists of elements with increasing
                 * currents. After a set, the current is reset, and 10 more tests are run.
                **/
                XmlNode stimulusSetNode = doc.CreateElement("StimulusSet");
                XmlAttribute stimulusSetAttribute = doc.CreateAttribute("id");
                stimulusSetNode.Attributes.Append(stimulusSetAttribute);

                foreach (DataElement element in column.colElements)
                {

                    // In other words, are we looking at the very first element?
                    // If so, create a new XML "element"
                    if (stimulusSet.index == 0 && stimulusSet.size == 0)
                    {
                        stimulusSetNode = doc.CreateElement("StimulusSet");
                        stimulusSetAttribute = doc.CreateAttribute("id");
                        stimulusSetAttribute.Value = stimulusSet.index.ToString();

                        stimulusSetNode.Attributes.Append(stimulusSetAttribute);
                        channelNode.AppendChild(stimulusSetNode);

                    }

                    /* 
                     * Are we looking at what should be the last element of the first stimulus set?
                     * This is determined by the row deficit
                     * 
                     **/
                    if (stimulusSet.index == 0 && ((stimulusSet.size == 10 - rowDeficit)))
                    {

                        stimulusSet.index++;
                        stimulusSet.size = 0;
                        stimulusSetNode = doc.CreateElement("StimulusSet");
                        stimulusSetAttribute = doc.CreateAttribute("id");
                        stimulusSetAttribute.Value = stimulusSet.index.ToString();

                        stimulusSetNode.Attributes.Append(stimulusSetAttribute);
                        channelNode.AppendChild(stimulusSetNode);

                    }
                    

                    /*
                     * If this is any set other than the first, it's size should be 10.
                     * 
                     **/
                    else if (stimulusSet.size == 10)
                    {
                        stimulusSet.index++;
                        stimulusSet.size = 0;
                        stimulusSetNode = doc.CreateElement("StimulusSet");
                        stimulusSetAttribute = doc.CreateAttribute("id");
                        stimulusSetAttribute.Value = stimulusSet.index.ToString();

                        stimulusSetNode.Attributes.Append(stimulusSetAttribute);
                        channelNode.AppendChild(stimulusSetNode);

                    }

                    XmlNode timeSliceNode = doc.CreateElement("Time");
                    XmlAttribute timeSliceAttributeIndex = doc.CreateAttribute("index");
                    XmlAttribute timeSliceAttributeTime = doc.CreateAttribute("time");

                    // We want to shift the time slice index to account for missing rows
                    // This way we can safely match up indices within each stimulus set in 
                    // order to take averages and deal with the data in general

                    timeSliceAttributeIndex.Value = (timeSliceIndex + rowDeficit).ToString();
                    timeSliceAttributeTime.Value = timeSlices[timeSliceIndex].ToString();
                    timeSliceNode.Attributes.Append(timeSliceAttributeIndex);
                    timeSliceNode.Attributes.Append(timeSliceAttributeTime);


                    timeSliceNode.AppendChild(doc.CreateTextNode(element.value.ToString()));
                    stimulusSetNode.AppendChild(timeSliceNode);

                    timeSliceIndex++;
                    stimulusSet.size++;
                }

                channelIndex++;
            }

            string outFileName = inFile.Substring(0, inFile.LastIndexOf("."));
            //doc.Save(directory + outFileName + ".xml");
            using (TextWriter sw = new StreamWriter(inDirectory + outFileName + ".xml", false, Encoding.UTF8)) //Set encoding
            {
                doc.Save(sw);
            }

            #endregion
        }
    }
}
