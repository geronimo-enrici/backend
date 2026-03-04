# 1. SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiamos todo el contenido del repositorio
COPY . .

# Buscamos CUALQUIER archivo .csproj y hacemos el restore
RUN dotnet restore $(find . -name "*.csproj")

# Publicamos el proyecto buscando el archivo .csproj automáticamente
RUN dotnet publish $(find . -name "*.csproj") -c Release -o out

# 2. Runtime para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Exponemos el puerto
EXPOSE 80
EXPOSE 8080

# El nombre del DLL suele ser el mismo que el del proyecto
# Si tu proyecto se llama "prueba", esto funcionará:
ENTRYPOINT ["dotnet", "prueba.dll"]
