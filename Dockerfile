# Usa la imagen oficial de .NET Core SDK para compilar y ejecutar tu aplicación
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Usa la imagen oficial de .NET Core SDK para la construcción
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TodoListApi.csproj", "./"]
RUN dotnet restore "TodoListApi.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "TodoListApi.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
RUN dotnet publish "TodoListApi.csproj" -c Release -o /app/publish

# Usa una imagen base para ejecutar la aplicación
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoListApi.dll"]
