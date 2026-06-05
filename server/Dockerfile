FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN if [ -f API/API.csproj ]; then \
			dotnet restore API/API.csproj; \
		elif [ -f server/API/API.csproj ]; then \
			dotnet restore server/API/API.csproj; \
		else \
			echo "API.csproj not found. Check build context." >&2; exit 1; \
		fi
RUN if [ -f API/API.csproj ]; then \
			dotnet publish API/API.csproj -c Release -o /app/publish; \
		elif [ -f server/API/API.csproj ]; then \
			dotnet publish server/API/API.csproj -c Release -o /app/publish; \
		else \
			echo "API.csproj not found. Check build context." >&2; exit 1; \
		fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5432
EXPOSE 5432

ENTRYPOINT ["dotnet", "API.dll"]