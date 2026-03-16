# 1. SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copiamos TODO el contenido de tu repositorio al contenedor
COPY . .

# Ejecutamos el restore apuntando específicamente a la carpeta donde está el proyecto
# (Ajustado a tu carpeta "prueba")
RUN dotnet restore "prueba/prueba.csproj"

# Publicamos el proyecto
RUN dotnet publish "prueba/prueba.csproj" -c Release -o out

# 2. Runtime para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Render usa puertos dinámicos, pero exponemos los comunes
EXPOSE 80
EXPOSE 8080

ENTRYPOINT ["dotnet", "prueba.dll"]