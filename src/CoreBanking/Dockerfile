FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj e restaurar (cache layer)
COPY src/CoreBanking/CoreBanking.csproj CoreBanking/
RUN dotnet restore CoreBanking/CoreBanking.csproj

# Copiar tudo e buildar
COPY src/CoreBanking/ CoreBanking/
RUN dotnet publish CoreBanking/CoreBanking.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5001
ENV ASPNETCORE_URLS=http://0.0.0.0:5001
ENTRYPOINT ["dotnet", "CoreBanking.dll"]
