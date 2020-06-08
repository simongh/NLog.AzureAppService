using NLog.Targets;
using System.IO;

namespace NLog.Extensions.AzureAppService
{
	[Target("AzureAppServicesFile")]
	public class FilesTarget : FileTarget
	{
		public FilesTarget()
		{
			var path = ".\\";
			if (AppContext.Current.IsRunningInAzure)
			{
				path = Path.Combine(AppContext.Current.HomeFolder, "LogFiles", "Application");
			}
			FileName = new Layouts.SimpleLayout($"{path}\\diagnostics-${{date:universalTime=true:format=yyyyMMdd}}.txt");
			MaxArchiveFiles = 2;
			ArchiveAboveSize = 10 * 1024 * 1024;
		}

		protected override void Write(LogEventInfo logEvent)
		{
			if (!AppContext.Current.IsRunningInAzure)
				return;

			base.Write(logEvent);
		}
	}
}