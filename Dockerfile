# ETAPA 1: Compilación (SDK 6.0)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copiamos todo el contenido para que encuentre el .csproj
COPY . .

# Restauramos y publicamos buscando el archivo .csproj automáticamente
RUN dotnet restore $(find . -name "*.csproj")
RUN dotnet publish $(find . -name "*.csproj") -c Release -o out

# ETAPA 2: Ejecución (Runtime 6.0 - Esto es lo que te faltaba)
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Render usa puertos dinámicos, pero exponemos los estándar
EXPOSE 80
EXPOSE 8080

# Comando para iniciar la app (asegúrate de que el archivo sea prueba.dll)
ENTRYPOINT ["dotnet", "prueba.dll"]
