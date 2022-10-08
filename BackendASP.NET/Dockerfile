#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BackendASP.NET/BackendASP.NET.csproj", "BackendASP.NET/"]
RUN dotnet restore "BackendASP.NET/BackendASP.NET.csproj"
COPY . .
WORKDIR "/src/BackendASP.NET"
RUN dotnet build "BackendASP.NET.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BackendASP.NET.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackendASP.NET.dll"]