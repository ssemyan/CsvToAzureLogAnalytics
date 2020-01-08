using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CsvToAzureLogAnalytics
{
	public class LogAnalyticsDataCollector
	{
		// Log Analytics workspace ID
		private readonly string _workspaceId;

		// For sharedKey, use either the primary or the secondary Connected Sources client authentication key   
		private readonly string _workspaceKey;

		// LogName is name of the event type that is being submitted to Azure Monitor
		private readonly string _logName;

		// You can use an optional field to specify the timestamp from the data. If the time field is not specified, Azure Monitor assumes the time is the message ingestion time
		private readonly string _timeStampField;

		public LogAnalyticsDataCollector(string customerId, string sharedKey, string logName, string timeStampField)
		{
			_workspaceId = customerId;
			_workspaceKey = sharedKey;
			_logName = logName;
			_timeStampField = timeStampField;
		}

		// Build the API signature
		private static string BuildSignature(string message, string secret)
		{
			var encoding = new ASCIIEncoding();
			byte[] keyByte = Convert.FromBase64String(secret);
			byte[] messageBytes = encoding.GetBytes(message);
			using (var hmacsha256 = new HMACSHA256(keyByte))
			{
				byte[] hash = hmacsha256.ComputeHash(messageBytes);
				return Convert.ToBase64String(hash);
			}
		}

		// Send a request to the POST API endpoint
		public async Task<HttpResponseMessage> PostData(string json)
		{
			// Create a hash for the API signature
			var datestring = DateTime.UtcNow.ToString("r");
			var jsonBytes = Encoding.UTF8.GetBytes(json);
			string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
			string hashedString = BuildSignature(stringToHash, _workspaceKey);
			string signature = "SharedKey " + _workspaceId + ":" + hashedString;

			string url = "https://" + _workspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

			HttpClient client = new HttpClient();
			client.DefaultRequestHeaders.Add("Accept", "application/json");
			client.DefaultRequestHeaders.Add("Log-Type", _logName);
			client.DefaultRequestHeaders.Add("Authorization", signature);
			client.DefaultRequestHeaders.Add("x-ms-date", datestring);
			client.DefaultRequestHeaders.Add("time-generated-field", _timeStampField);

			HttpContent httpContent = new StringContent(json, Encoding.UTF8);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return await client.PostAsync(new Uri(url), httpContent);
		}
	}
}
