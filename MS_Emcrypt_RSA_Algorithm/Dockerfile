#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MS_Emcrypt_RSA_Algorithm/MS_Emcrypt_RSA_Algorithm.csproj", "MS_Emcrypt_RSA_Algorithm/"]
RUN dotnet restore "./MS_Emcrypt_RSA_Algorithm/MS_Emcrypt_RSA_Algorithm.csproj"
COPY . .
WORKDIR "/src/MS_Emcrypt_RSA_Algorithm"
RUN dotnet build "./MS_Emcrypt_RSA_Algorithm.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MS_Emcrypt_RSA_Algorithm.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MS_Emcrypt_RSA_Algorithm.dll"]