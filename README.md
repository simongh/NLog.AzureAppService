# NLog.AzureAppService [![Build status](https://ci.appveyor.com/api/projects/status/yoqk7t0ag37tlgvi?svg=true)](https://ci.appveyor.com/project/SimonHalsey/nlog-azureappservice)

An NLog target for Azure App Services.
This target allows App Services running on Azure to log to the configured endpoints. Using the diagnostic settings in the app Service, configure the blob storage settings.

Log messages are batch added for performance.

Install the package from nuget **link TBC**

## Sample Configuration

```xml
<extensions>
    <add assembly="nlog.extensions.azureappservice" />
</extensions>

<targets async="true">
    <target type="AzureAppServicesBlob"
            name="azBlob"
            layout="${longdate:universalTime=true} ${level:uppercase=true} ${message} ${exception:format=tostring}" />
    <target type="AzureAppServicesFile"
            name="azFile"
            layout="${longdate:universalTime=true} ${level:uppercase=true} ${message} ${exception:format=tostring}"/>
</targets>
```
