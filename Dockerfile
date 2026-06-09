# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 80

# SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["pharmEasyClone-backend.csproj", "./"]
RUN dotnet restore "pharmEasyClone-backend.csproj"
COPY . .
RUN dotnet build "pharmEasyClone-backend.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "pharmEasyClone-backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pharmEasyClone-backend.dll"]
