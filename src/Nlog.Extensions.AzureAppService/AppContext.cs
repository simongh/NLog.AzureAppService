using System;

namespace NLog.Extensions.AzureAppService
{
	internal class AppContext : IAppContext
	{
		public static AppContext Current { get; } = new AppContext();

		private AppContext()
		{
		}

		public string HomeFolder { get; } = Environment.GetEnvironmentVariable("HOME");

		public string SiteName { get; } = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

		public string SiteInstanceId { get; } = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");

		public bool IsRunningInAzure => !string.IsNullOrEmpty(HomeFolder) && !string.IsNullOrEmpty(SiteName);
	}
}