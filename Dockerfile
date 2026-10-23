# ==========================================
# ETAPA 1: Construcción (Build Stage .NET 10 SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar proyectos para restaurar dependencias aprovechando cache de capas
COPY ["src/Firmeza.Domain/Firmeza.Domain.csproj", "src/Firmeza.Domain/"]
COPY ["src/Firmeza.Application/Firmeza.Application.csproj", "src/Firmeza.Application/"]
COPY ["src/Firmeza.Infrastructure/Firmeza.Infrastructure.csproj", "src/Firmeza.Infrastructure/"]
COPY ["src/Firmeza.Presentation/Firmeza.Presentation.csproj", "src/Firmeza.Presentation/"]

RUN dotnet restore "src/Firmeza.Presentation/Firmeza.Presentation.csproj"

# Copiar todo el código fuente
COPY src/ src/

WORKDIR "/src/src/Firmeza.Presentation"
RUN dotnet publish "Firmeza.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# ETAPA 2: Runtime de Producción (.NET 10 ASP.NET)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Puerto HTTP estándar por defecto en .NET 8/9/10
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Firmeza.Presentation.dll"]
