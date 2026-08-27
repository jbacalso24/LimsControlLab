# syntax=docker/dockerfile:1
# Multi-stage build for the LimsControlLab .NET 10 API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the solution so project references resolve.
COPY LimsControlLab.sln ./
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet restore src/LimsControlLab.Api/LimsControlLab.Api.csproj
RUN dotnet publish src/LimsControlLab.Api/LimsControlLab.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Render provides $PORT; bind Kestrel to it. HTTPS is terminated at Render's edge.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LimsControlLab.Api.dll"]
