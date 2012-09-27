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

        public static void TextToXml(string inDirectory, string inFile)
        {
            List<string> channelNames = new List<string>();

            List<double> timeSlices = new List<double>();

            List<Column> colDataSet = new List<Column>();

            string directory = inDirectory;
            string fileName = inFile;
            StreamReader reader = new StreamReader(directory + fileName);

            // Header line looks like "T[s]\tName\tName\tName"
            // Read the header, skip over the T[s] portion. Then put the names into a list
            // At the same time, use the header channel names to determine the number
            // of columns in the file
            int columnCount = 0;
            foreach (string name in reader.ReadLine().Split('\t'))
            {
                if (name == "T[s]")
                    continue;
                channelNames.Add(name);
                columnCount++;
            }

            // *******************************************************************
            // Empty line between header and data. Skip over empty line
            // Only sometimes ????
            //reader.ReadLine();
            // *******************************************************************

            /**
             * For every column in the file (found by counting header names),
             * generate a new Column object to put into the overall data set
             **/
            for (int i = 0; i < columnCount; i++)
            {
                Column column = new Column();
                column.index = i;
                column.colElements = new List<DataElement>();

                colDataSet.Add(column);
            }


            /**
             * Each data row looks like "Time\tValue\tValue\tValue"
             * Pull out time, then get data
            **/

            // RowIndex quite simply keeps track of what row is being read in the file. We need
            // this to determine where to place the data within the column element list.
            int rowIndex = 0;
            int totalRows;

            // Loop through all of the rows in the file
            while (!reader.EndOfStream)
            {
                // read in the row
                string row = reader.ReadLine();
                
                // if any rows after the headline row is empty, skip it
                if (row == string.Empty)
                    continue;

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

                // Place one piece of data in each column for this particular row
                for (int i = 0; i < columnCount; i++)
                {
                    colDataSet[i].colElements.Add(new DataElement());
                    colDataSet[i].colElements[rowIndex].value = Convert.ToDouble(rowValues[i]);
                }

                rowIndex++;
            }
            
            // after looping through the rows in the file
            totalRows = rowIndex;

            //write xml data
            XmlDocument doc = new XmlDocument();
            XmlNode declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlNode fileNameNode = doc.CreateElement("File_Name");
            XmlAttribute fileNameAttribute = doc.CreateAttribute("value");
            fileNameAttribute.Value = fileName;
            fileNameNode.Attributes.Append(fileNameAttribute);
            doc.AppendChild(fileNameNode);

            /**
             * int channelIndex = 0;
             * int timeSliceIndex = 0;
             * foreach(column in colDataSet)
             *  add channel name node
             *  foreach(dataelement in column)
             *      add time in to file
             *      value = dataelement.value
             **/
            int channelIndex = 0;

            //
            int rowDeficit = 10 - (rowIndex  % 10);

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
                    
                    /**
                     * working-ish!
                    if ((timeSliceIndex % (10-rowDeficit) == 0) && stimulusSetIndex == 0) 
                    {
                        Console.WriteLine("10=-rowDeficit: " + (10 - rowDeficit).ToString());
                        stimulusSetNode = doc.CreateElement("StimulusSet");
                        stimulusSetAttribute = doc.CreateAttribute("id");
                        stimulusSetAttribute.Value = stimulusSetIndex.ToString();

                        stimulusSetNode.Attributes.Append(stimulusSetAttribute);
                        channelNode.AppendChild(stimulusSetNode);

                        stimulusSetIndex++;
                    }
                    else if ((timeSliceIndex % 10) == 0)
                    {
                        stimulusSetIndex++;
                        stimulusSetNode = doc.CreateElement("StimulusSet");
                        stimulusSetAttribute = doc.CreateAttribute("id");
                        stimulusSetAttribute.Value = stimulusSetIndex.ToString();

                        stimulusSetNode.Attributes.Append(stimulusSetAttribute);
                        channelNode.AppendChild(stimulusSetNode);
                    }
                     **/
                    
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
                    if (stimulusSet.index == 0 && ( (stimulusSet.size == 10-rowDeficit) ))
                    {

                        stimulusSet.index++;
                        stimulusSet.size = 0;
                        stimulusSetNode = doc.CreateElement("StimulusSet");
                        stimulusSetAttribute = doc.CreateAttribute("id");
                        stimulusSetAttribute.Value = stimulusSet.index.ToString();

                        stimulusSetNode.Attributes.Append(stimulusSetAttribute);
                        channelNode.AppendChild(stimulusSetNode);

                    }

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

                    //else if (element.index % 10 == 0)
                    XmlNode timeSliceNode = doc.CreateElement("Time");
                    XmlAttribute timeSliceAttributeIndex = doc.CreateAttribute("index");
                    XmlAttribute timeSliceAttributeTime = doc.CreateAttribute("time");
                    timeSliceAttributeIndex.Value = timeSliceIndex.ToString();
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

            string outFileName = fileName.Substring(0, fileName.LastIndexOf("."));
            //doc.Save(directory + outFileName + ".xml");
            using (TextWriter sw = new StreamWriter(directory + outFileName + ".xml", false, Encoding.UTF8)) //Set encoding
            {
                doc.Save(sw);
            }
        }
    }
}
