FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80 6666

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["VrServer/VrServer.csproj", "VrServer/"]
RUN dotnet restore "VrServer/VrServer.csproj"
COPY . .
WORKDIR "/src/VrServer"
RUN dotnet build "VrServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "VrServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "VrServer.dll"]