# 1️⃣ Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out

# 2️⃣ Финальный контейнер
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /out .
ENTRYPOINT ["dotnet", "DevOilBot_ForAdmins.dll"]
