# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY ["ORYS.WebApi/ORYS.WebApi.csproj", "ORYS.WebApi/"]
RUN dotnet restore "ORYS.WebApi/ORYS.WebApi.csproj"

# Tüm dosyaları kopyala ve yayınla
COPY . .
WORKDIR "/src/ORYS.WebApi"
RUN dotnet publish "ORYS.WebApi.csproj" -c Release -o /app/publish

# Çalıştırma aşaması
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render'ın verdiği portu kullan
ENV ASPNETCORE_URLS=http://+:5050
EXPOSE 5050

ENTRYPOINT ["dotnet", "ORYS.WebApi.dll"]
