FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/InsightVault.Domain/InsightVault.Domain.csproj", "src/InsightVault.Domain/"]
COPY ["src/InsightVault.Application/InsightVault.Application.csproj", "src/InsightVault.Application/"]
COPY ["src/InsightVault.Infrastructure/InsightVault.Infrastructure.csproj", "src/InsightVault.Infrastructure/"]
COPY ["src/InsightVault.Api/InsightVault.Api.csproj", "src/InsightVault.Api/"]

RUN dotnet restore "src/InsightVault.Api/InsightVault.Api.csproj"

COPY . .
RUN dotnet publish "src/InsightVault.Api/InsightVault.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InsightVault.Api.dll"]
