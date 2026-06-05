# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NexoraAPI/NexoraAPI.csproj", "NexoraAPI/"]
RUN dotnet restore "NexoraAPI/NexoraAPI.csproj"

COPY NexoraAPI/ NexoraAPI/
WORKDIR "/src/NexoraAPI"
RUN dotnet publish "NexoraAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NexoraAPI.dll"]
