FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_HTTP_TIMEOUT=600
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bank.Obs.SqlPoller.dll"]