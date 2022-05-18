#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.Service/Tgs.Promotion.Service.csproj", "Tgs.Promotion.Service/"]
COPY ["Tgs.Promotion.Domain/Tgs.Promotion.Domain.csproj", "Tgs.Promotion.Domain/"]
COPY ["Tgs.Promotion.Core/Tgs.Promotion.Core.csproj", "Tgs.Promotion.Core/"]
COPY ["Tgs.Promotion.Repository/Tgs.Promotion.Repository.csproj", "Tgs.Promotion.Repository/"]
RUN dotnet restore "Tgs.Promotion.Service/Tgs.Promotion.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.Service"
RUN dotnet build "Tgs.Promotion.Service.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.Service.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.Service.dll"]