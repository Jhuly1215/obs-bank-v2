# 1. Build Layer
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_HTTP_TIMEOUT=600
WORKDIR /src
COPY ["Bank.Obs.FcmBridge.csproj", "./"]
RUN dotnet restore "Bank.Obs.FcmBridge.csproj"
COPY . .
RUN dotnet build "Bank.Obs.FcmBridge.csproj" -c Release -o /app/build

# 2. Publish Layer
FROM build AS publish
RUN dotnet publish "Bank.Obs.FcmBridge.csproj" -c Release -o /app/publish

# 3. Runtime Layer
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Bank.Obs.FcmBridge.dll"]
