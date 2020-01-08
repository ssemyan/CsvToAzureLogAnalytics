using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace CsvToAzureLogAnalytics
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get filename from arguments
            if (args.Length != 1)
            {
                Console.WriteLine("Usage:\n\tCsvToAzureLogAnalytics.exe [name of csv file to process]");
                return;
            }
            var fileName = args[0];
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File {fileName} does not exist");
                return;
            }

            // Get values from config
            var config = new ConfigurationBuilder()
             .SetBasePath(AppContext.BaseDirectory)
             .AddJsonFile("appsettings.json", optional: false)
             .Build();

            var workspaceId = config["WorkspaceId"];
            var workspaceKey = config["WorkspaceKey"];
            var logName = config["LogName"];
            var timeStampField = config["TimeStampField"];
            var batchSizeStr = config["BatchSize"];

            // Make sure config set correctly
            if (string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(workspaceKey) || string.IsNullOrEmpty(logName) || string.IsNullOrEmpty(timeStampField) || string.IsNullOrEmpty(batchSizeStr))
            {
                Console.WriteLine("Values missing from appsettings.json");
                return;
            }

            // Make sure batch size is an int
            if (!int.TryParse(batchSizeStr, out int batchSize))
            {
                Console.WriteLine("BatchSize is not a number");
                return;
            }

            // Create Data Collector
            var collector = new LogAnalyticsDataCollector(workspaceId, workspaceKey, logName, timeStampField);

            string[] properties = null;
            using (TextFieldParser fieldParser = new TextFieldParser(fileName))
            {
                // Configure the TextFieldParser
                // This tells the parser it is a Delimited text 
                fieldParser.TextFieldType = FieldType.Delimited;
                // This tells the parser that delimiter used is a ,
                fieldParser.Delimiters = new string[] { "," };
                // This tells the parser that some fields may have quotes around them
                fieldParser.HasFieldsEnclosedInQuotes = true;
                // Used to hold the fields after each read
                string[] fields;

                // Read and process the fields
                int totalCount = 0;
                int curBatchSize = 0;
                var listObjResult = new List<Dictionary<string, object>>();
                while (!fieldParser.EndOfData)
                {
                    // Parse the line and read into the array
                    fields = fieldParser.ReadFields();
                    if (properties == null)
                    {
                        properties = fields;
                    }
                    else
                    {
                        totalCount++;
                        var objResult = new Dictionary<string, object>();
                        for (int j = 0; j < properties.Length; j++)
                        {
                            // check if record is number or datetime
                            string rec = fields[j];
                            if (double.TryParse(rec, out double rec_d))
                            {
                                // Record is number
                                objResult.Add(properties[j], rec_d);
                            }
                            else if (DateTime.TryParse(rec, out DateTime rec_t))
                            {
                                // Record is datetime
                                objResult.Add(properties[j], rec_t);
                            }
                            else
                            {
                                // Record is string
                                objResult.Add(properties[j], rec);
                            }
                        }

                        listObjResult.Add(objResult);
                        curBatchSize++;

                        // Send out current batch 
                        if (curBatchSize >= batchSize)
                        {
                            SendLogs(collector, listObjResult, totalCount);
                            listObjResult.Clear();
                            curBatchSize = 0;
                        }
                    }
                }
                // Send any logs
                SendLogs(collector, listObjResult, totalCount);
            }
        }

        private static void SendLogs(LogAnalyticsDataCollector collector, List<Dictionary<string, object>> listObjResult, int totalCount)
        {
            // Convert to JSON
            string jsonOut = JsonConvert.SerializeObject(listObjResult);
            
            // Send
            Console.Write($"Sending {totalCount}...");
            var result = collector.PostData(jsonOut).Result;
            result.EnsureSuccessStatusCode();
            Console.WriteLine($" done.");
        }
    }
}
