# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiuj ca?o?? i przywró?
COPY . .
RUN dotnet restore

# Publish
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:80
# Domy?lny connection string wskazuje na us?ug? "db" z docker-compose
ENV ConnectionStrings__Default="Host=db;Database=appdb;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

EXPOSE 80
ENTRYPOINT ["dotnet", "MyMvcPostgresApp.dll"]