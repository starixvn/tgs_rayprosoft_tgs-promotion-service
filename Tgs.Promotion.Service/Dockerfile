#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.Service/Tgs.Promotion.Service.csproj", "Tgs.Promotion.Service/"]
COPY ["Tgs.Promotion.Cache/Tgs.Promotion.Cache.csproj", "Tgs.Promotion.Cache/"]
COPY ["Tgs.Promotion.Core/Tgs.Promotion.Core.csproj", "Tgs.Promotion.Core/"]
COPY ["Tgs.Promotion.Common/Tgs.Promotion.Common.csproj", "Tgs.Promotion.Common/"]
COPY ["Tgs.Promotion.Domain/Tgs.Promotion.Domain.csproj", "Tgs.Promotion.Domain/"]
COPY ["Tgs.Promotion.Repository/Tgs.Promotion.Repository.csproj", "Tgs.Promotion.Repository/"]
RUN dotnet restore "./Tgs.Promotion.Service/./Tgs.Promotion.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.Service"
RUN dotnet build "./Tgs.Promotion.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Promotion.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.Service.dll"]