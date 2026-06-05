FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all source code
COPY . .

# Publish the API project
WORKDIR /src/API
RUN dotnet publish "API.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Expose the default ASP.NET Core port
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]
