#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-preview.4-trixie-slim-arm64v8 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0.100-preview.4-alpine3.21 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/GamingWebApp/GamingWebApp.csproj", "src/GamingWebApp/"]
RUN dotnet restore "src/GamingWebApp/GamingWebApp.csproj"
RUN dotnet dev-certs https --trust
COPY . .
WORKDIR "/src/src/GamingWebApp"
RUN dotnet build "GamingWebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GamingWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GamingWebApp.dll"]