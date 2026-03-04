# 1. SDK para compilar (Cambiado a 6.0)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copiamos todo el contenido del repositorio
COPY . .

# Buscamos el archivo .csproj y hacemos el restore
RUN dotnet restore $(find . -name "*.csproj")

# Publicamos el proyecto
RUN dotnet publish $(find . -name "*.csproj") -c Release -o out

# 2. Runtime para ejecutar (Cambiado a 6.0)
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Exponemos los puertos
EXPOSE 80
EXPOSE 8080

# Comando de inicio (asegúrate que se llame prueba.dll)
ENTRYPOINT ["dotnet", "prueba.dll"]
