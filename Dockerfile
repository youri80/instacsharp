FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 3000

RUN apt-get update && apt-get install -y \
    chromium --no-install-recommends && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENTRYPOINT ["dotnet", "Instagramcsharp.dll"]
