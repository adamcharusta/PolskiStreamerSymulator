# This build becomes usable when PolskiStreamerSymulatorApp/src/Server exists.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish PolskiStreamerSymulatorApp/src/Server/Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER 1654
EXPOSE 8080
ENTRYPOINT ["dotnet", "Server.dll"]
