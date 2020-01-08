# CsvToAzureLogAnalytics
Command line tool to load CSV file into Azure Monitor Log Analytics

## Settings
First you must update the settings in the *appsettings.json* file:

* WorkspaceId - ID of the Log Analytics Workspace. This can be found in the Advanced settings / Connected Sources / Windows Servers pane
* WorkspaceKey - Key for the Log Analytics Workspace. Found on the same location as the WorkspaceID. Can 
* LogName - Name of the log the data will be uploaded to. On the first upload datatypes are determined and subsequent uploads will use the same data types.
* TimeStampField - Name of the column to use as the Timestamp
* BatchSize - Size of batches of records to send - e.g. 1000. Set too high will result in errors.

## To Use on the command line

    CsvToAzureLogAnalytics.exe [Name of CSV file to upload]
    
The CSV file will be loaded and the first line will be used as column headers. These headers are used as the data names in Log Analytics. Data values will be attempted to be cast to a number or date/time, otherwise will be assumed to be strings. 

The upload to a Log Analytics workspace is done in batches. A typical batch size is 1000 records at a time.

**Note:** when uploading any data into Log Analytics, no check is done for duplicates. Thus, if you upload the same file twice, you will see the duplicate data in the table. 
