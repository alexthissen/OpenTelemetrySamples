# OpenTelemetry Samples

This repository contains instructions to run the OpenTelemetry sample application.

## ASP.NET Core developer certificate for HTTPS traffic 

When using containers during development, the local developer certificate needs to be mapped into the container running ASP.NET Core. In Visual Studio 2022 and 2026 this is handled by mapping environment variables and volume mounts. However, in Visual Studio Code the container tooling is not available to create the required Docker (Compose) files.

From the root of your cloned repository, run the following command:

```
dotnet dev-certs https --export-path ./certificate.pfx --password <your-password> --trust
```

Use the chosen password in the Docker Compose override file `docker-compose.override.yml` to indicate the password for the configuration of Kestrel. Update the value for the password in the 

```
- ASPNETCORE_Kestrel__Certificates__Default__Password=<your-password>
```

Alternatively, you could use the provided `certificate.pfx` file with its password `9b1c92b7-011c-4724-86c3-82a643b1a242`, but you would have to trust and install this certificate in your certificate store replacing an existing one. This approach is not recommended, unless you did not have a dev-cert yet. 

