
# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Starter.Api/Starter.Api.csproj Starter.Api/
RUN dotnet restore Starter.Api/Starter.Api.csproj
COPY . .
RUN dotnet publish Starter.Api/Starter.Api.csproj -c Release -o /app/publish

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Starter.Api.dll"]
