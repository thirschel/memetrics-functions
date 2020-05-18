# Starting from MS's dotnet image that has all the SDKs installed,
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

COPY . /
RUN cd /src/MeMetrics.Updater.Functions && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# Azure functions use a special runtime
FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice

# Set up the app to run
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build ["/home/site/wwwroot", "/home/site/wwwroot"]