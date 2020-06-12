<h1 align="center">A digitial-life viewing application</h1>

<h3 align="center">
  <a href="https://memetrics.net/">Visit MeMetrics</a>
</h3>

<h3 align="center">
  <a href="https://github.com/thirschel/memetrics-ui/blob/master/ARCHITECTURE.md">Architecture Diagram</a> |
  <a href="https://github.com/thirschel/memetrics-ui">UI</a> |
  <a href="https://github.com/thirschel/memetrics-infrastructure">Infrastructure</a> |
  <a href="https://github.com/thirschel/memetrics-api">API</a> | 
  <a href="https://github.com/thirschel/memetrics-imessage-updater">iMessage Updater</a>
</h3>

## What is this?

This project contains the Azure timer function that reaches out to the various online services for data. 
The function runs on an interval and collects data that occurred in the past fews days and sends it to the MeMetrics Api to be ingested and saved to the database.
Once it has successfully sent all the data to MeMetrics for an interval, it will then tell the MeMetrics api to cache the results for 24 hours.

## Technology / Methodology
- .NET Core 3.1
- Azure Functions v3
- Automapper
- Terraform
- Docker
- Azure Pipelines

## Setting up development environment 🛠

This project can be run using any apporach a normal .NET core application could be run (VS, VS Code, dotnet sdk) as well as using the Dockerfile to build an image.

The environment variables specified [here](https://github.com/thirschel/memetrics-functions/blob/master/src/MeMetrics.Updater.Application/Objects/EnvironmentConfiguration.cs) need to be supplied (via `appsettings.json`, ENV variables, etc)
