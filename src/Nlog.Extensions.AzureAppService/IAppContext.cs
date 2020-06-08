namespace NLog.Extensions.AzureAppService
{
	internal interface IAppContext
	{
		string HomeFolder { get; }
		bool IsRunningInAzure { get; }
		string SiteInstanceId { get; }
		string SiteName { get; }
	}
}