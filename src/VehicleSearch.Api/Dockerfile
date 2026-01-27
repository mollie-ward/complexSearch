FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["VehicleSearch.Api/VehicleSearch.Api.csproj", "VehicleSearch.Api/"]
COPY ["VehicleSearch.Core/VehicleSearch.Core.csproj", "VehicleSearch.Core/"]
COPY ["VehicleSearch.Infrastructure/VehicleSearch.Infrastructure.csproj", "VehicleSearch.Infrastructure/"]
COPY ["VehicleSearch.Agents/VehicleSearch.Agents.csproj", "VehicleSearch.Agents/"]
COPY ["VehicleSearch.ServiceDefaults/VehicleSearch.ServiceDefaults.csproj", "VehicleSearch.ServiceDefaults/"]
RUN dotnet restore "VehicleSearch.Api/VehicleSearch.Api.csproj"
COPY . .
WORKDIR "/src/VehicleSearch.Api"
RUN dotnet build "VehicleSearch.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VehicleSearch.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VehicleSearch.Api.dll"]
