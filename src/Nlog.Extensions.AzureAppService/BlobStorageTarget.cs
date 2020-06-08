using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NLog.Extensions.AzureAppService
{
	[Target("AzureAppServicesBlob")]
	public sealed class BlobStorageTarget : AsyncTaskTarget
	{
		private readonly HttpClient _client;
		private string _blobName = "applicationLog.txt";
		private Uri _appendUri;
		private Uri _fullUri;

		private string ContainerUrl => AppContext.Current.ContainerUrl;

		private string ApplicationInstanceId => AppContext.Current.SiteInstanceId;

		private string AppName => AppContext.Current.SiteName;

		private string FileName => $"{ApplicationInstanceId}_{BlobName}";

		public string BlobName
		{
			get => _blobName;
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException(nameof(value), $"{nameof(BlobName)} must be a non-empty string");

				_blobName = value;
			}
		}

		public BlobStorageTarget()
			: base()
		{
			_client = new HttpClient();
			BatchSize = 1000;
			QueueLimit = 1000;
			TaskDelayMilliseconds = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
		}

		protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		protected async override Task WriteAsyncTask(IList<LogEventInfo> logEvents, CancellationToken cancellationToken)
		{
			if (!AppContext.Current.IsRunningInAzure)
				return;

			var eventGroups = logEvents.GroupBy(GetBlobKey);
			foreach (var group in eventGroups)
			{
				var key = group.Key;
				var blobName = $"{AppName}/{key.Year}/{key.Month:00}/{key.Day:00}/{key.Hour:00}/{FileName}";

				var builder = new UriBuilder(ContainerUrl);
				builder.Path += "/" + blobName;

				_fullUri = builder.Uri;
				AppendBlockQuery(builder);
				_appendUri = builder.Uri;

				using (var stream = new MemoryStream())
				{
					using (var writer = new StreamWriter(stream))
					{
						foreach (var item in group)
						{
							await writer.WriteAsync(RenderLogEvent(Layout, item));
						}

						await writer.FlushAsync();
						stream.TryGetBuffer(out var buffer);
						await AppendAsync(buffer, cancellationToken);
					}
				}
			}
		}

		private async Task AppendAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
		{
			Task<HttpResponseMessage> AppendDataAsync()
			{
				var message = new HttpRequestMessage(HttpMethod.Put, _appendUri)
				{
					Content = new ByteArrayContent(data.Array, data.Offset, data.Count),
				};
				AddCommonHeaders(message);

				return _client.SendAsync(message, cancellationToken);
			}

			var response = await AppendDataAsync();
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				var message = new HttpRequestMessage(HttpMethod.Put, _fullUri)
				{
					Content = new ByteArrayContent(Array.Empty<byte>()),
					Headers =
					{
						{ "If-None-Match","*" },
					},
				};
				AddCommonHeaders(message);

				response = await _client.SendAsync(message, cancellationToken);

				if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.PreconditionFailed)
				{
					response = await AppendDataAsync();
				}

				response.EnsureSuccessStatusCode();
			}
		}

		private (int Year, int Month, int Day, int Hour) GetBlobKey(LogEventInfo logEvent)
		{
			return (logEvent.TimeStamp.Year,
				logEvent.TimeStamp.Month,
				logEvent.TimeStamp.Day,
				logEvent.TimeStamp.Hour);
		}

		private void AppendBlockQuery(UriBuilder builder)
		{
			var queryToAppend = "comp=appendblock";
			if (builder.Query != null && builder.Query.Length > 1)
				builder.Query = builder.Query.Substring(1) + "&" + queryToAppend;
			else
				builder.Query = queryToAppend;
		}

		private void AddCommonHeaders(HttpRequestMessage message)
		{
			message.Headers.Add("x-ms-blob-type", "AppendBlob");
			message.Headers.Add("x-ms-version", "2016-05-31");
			message.Headers.Date = DateTimeOffset.UtcNow;
		}
	}
}