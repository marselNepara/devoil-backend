# 1️⃣ Сборка проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out

# 2️⃣ Финальный контейнер (только рантайм)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /out .
EXPOSE 80

ENTRYPOINT ["dotnet", "DevOilApi.dll"]
