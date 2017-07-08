using System;
using System.Linq;

namespace EventManagmentImporter
{
    class MainClass
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static string con = System.Configuration.ConfigurationManager.ConnectionStrings["MDM"].ToString();

        class EtssEvent
        {
            public DateTime InsertDate { get; set; }                // Required
            public string meteridentifier { get; set; }             // Required
            public string MeterEventTypeDesc { get; set; }          // Reguired if you want GISMap Lat and Lon
            public string MeterEventType { get; set; }
            public string Longitude { get; set; }                   // Required
            public string Latitude { get; set; }                    // Require
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Load some data!");
            LoadData();
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        public static void LoadData()
        {
            //2015-03-06 16:20:23.670,300675,Condition Cleared,ME13-GE,-86.37566101,35.28058252
            var events = new System.Collections.Generic.List<EtssEvent>();
            string fileName = @"C:\Users\Craig Nicholson\Documents\Visual Studio 2015\Projects\CSharpReadCsvIntoObjectBulkInsert\EventManagmentImporter\MeterEvents.csv";
            foreach (var line in System.IO.File.ReadAllLines(fileName, System.Text.Encoding.GetEncoding(1250)).Skip(1))
            {
                string[] values = line.Split(',');
                int length = values.Length;
                EtssEvent e = new EtssEvent();
                if (length >= 0)
                    e.InsertDate = Convert.ToDateTime(values[0]);
                if (length > 1)
                    e.meteridentifier = values[1];
                if (length > 2)
                    e.MeterEventTypeDesc = values[2];
                if (length > 3)
                    e.MeterEventType = values[3];
                if (length > 4)
                    e.Longitude = values[4];
                if (length > 5)
                    e.Latitude = values[5];
                events.Add(e);
            }
            var dt = MapObjectToDataTable(events);
            Console.WriteLine("Insert some data");
            BulkInsertDataTable(dt, "EventAnalysis", con);
        }

        private static System.Data.DataTable MapObjectToDataTable(System.Collections.Generic.List<EtssEvent> data)
        {
            //[MeterIdentifier] [varchar] (50) NULL,
            //[AMISerialNo] [varchar] (50) NULL,
            //[LocationNumber] [varchar] (20) NULL,
            //[MapNumber] [varchar] (30) NULL,
            //[SubstationIdentifier] [varchar] (10) NULL,
            //[FeederIdentifier] [varchar] (20) NULL,
            //[TransformerIdentifier] [varchar] (20) NULL,
            //[Latitude] [varchar] (20) NULL,
            //[Longitude] [varchar] (20) NULL,
            //[EventDate] [datetime] NULL,
            //[EventType] [varchar] (50) NULL,
            //[EventTitle] [varchar] (50) NULL,
            //[Infor] [xml] NULL,
            //[CreatedBy] [varchar] (50) NULL,
            //[InsertDate] [datetime] NULL

            var dt = new System.Data.DataTable();
            int i = 0;
            string line = string.Empty;
            try
            {
                dt.Clear();
                dt.Columns.Add("MeterIdentifier", typeof(string));
                dt.Columns.Add("AMISerialNo", typeof(string));
                dt.Columns.Add("LocationNumber", typeof(string));
                dt.Columns.Add("MapNumber", typeof(string));
                dt.Columns.Add("SubstationIdentifier", typeof(string));
                dt.Columns.Add("FeederIdentifier", typeof(string));
                dt.Columns.Add("TransformerIdentifier", typeof(string));
                dt.Columns.Add("Latitude", typeof(string));
                dt.Columns.Add("Longitude", typeof(string));
                dt.Columns.Add("EventDate", typeof(DateTime));
                dt.Columns.Add("EventType", typeof(string));
                dt.Columns.Add("EventTitle", typeof(string));
                //dt.Columns.Add("Infor", typeof(System.Data.SqlTypes.SqlXml));
                dt.Columns.Add("Infor", typeof(string));
                dt.Columns.Add("CreatedBy", typeof(string));
                dt.Columns.Add("InsertDate", typeof(DateTime));

                //LocationNumber and SubstationIdentifier must have a value
                // Do the ReadValue Conversion Here too 

                foreach (EtssEvent read in data)
                {
                    // xmlformat can be many kvp's just adding one for grins.
                    // xml must be formatted properly, I had data with a comma so this has to stripped out or use CDATA
                    string xmlformat = "<Info><KeyValue key = \"{0}\" value = \"{1}\"/></Info>";
                    //string xml = string.Format (xmlformat, read.MeterEventType, read.MeterEventTypeDesc.Replace("\"", ""));
                    string xml = string.Format(xmlformat, "FAKE_KEY", "FAKE_VALUE");

                    //               i++;
                    //if (i > 900)
                    //	return dt;

                    System.Data.DataRow row = dt.NewRow();
                    row["MeterIdentifier"] = IsStringOverMaxLenCheck(read.meteridentifier,50);
                    row["AMISerialNo"] = IsStringOverMaxLenCheck(read.meteridentifier,50);

                    row["LocationNumber"] = (object)DBNull.Value; 
                    row["MapNumber"] = (object)DBNull.Value; 
                    row["SubstationIdentifier"] = (object)DBNull.Value;
                    row["FeederIdentifier"] = (object)DBNull.Value;
                    row["TransformerIdentifier"] = (object)DBNull.Value;

                    row["Latitude"] = !string.IsNullOrWhiteSpace(read.Latitude) ? (object)read.Latitude : (object)DBNull.Value;
                    row["Longitude"] = !string.IsNullOrWhiteSpace(read.Longitude) ? (object)read.Longitude : (object)DBNull.Value;
                    row["EventDate"] = read.InsertDate;
                    row["EventType"] = IsStringOverMaxLenCheck(read.MeterEventType,50);
                    row["EventTitle"] = IsStringOverMaxLenCheck(read.MeterEventTypeDesc.Replace("\"", ""),50);
                    row["Infor"] = xml;

                    row["CreatedBy"] = "EventManagmentImporter";
                    row["InsertDate"] = DateTime.Now;
                    dt.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("MapObjectToDataTable Error : {0}", ex.Message));
                logger.Error(string.Format("Counter : {0}", i));
            }
            return dt;
        }

        static string IsStringOverMaxLenCheck(string value, int maxlength)
        {
            if (value.Length > maxlength)
            {
                //We have so many of these it really slows the process down.
                if (true)
                    logger.Warn(string.Format("Truncated string: from {0} to {1}, value : {2}", value.Length, maxlength, value));
                return value.Substring(0, maxlength - 1);

            }
            else
                return value;
        }

        public static bool BulkInsertDataTable(System.Data.DataTable dt, string tableName, string _connectionString)
        {
            bool IsSuccess = false;
            try
            {
                using (System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(_connectionString))
                {
                    con.Open();
                    //TODO: See if using TableLock is good, bad, or does not matter.
                    using (System.Data.SqlClient.SqlBulkCopy bc = new System.Data.SqlClient.SqlBulkCopy(con, System.Data.SqlClient.SqlBulkCopyOptions.TableLock, null))
                    {
                        bc.DestinationTableName = tableName;
                        bc.BatchSize = dt.Rows.Count;
                        bc.BulkCopyTimeout = 0;
                        bc.WriteToServer(dt);
                    }
                    IsSuccess = true;
                    logger.ConditionalDebug(string.Format("Batch Complete"));
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("BulkInsertDataTable DataTable : {0}", dt.Rows.Count.ToString()));
                logger.Error(string.Format("BulkInsertDataTable Error : {0}", ex.Message));
                if (ex.InnerException != null)
                    logger.Error(string.Format("BulkInsertDataTable Inner : {0}", ex.InnerException.Message));
                throw ex;
            }
            return IsSuccess;
        }
    }
}
