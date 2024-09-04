# ===== Build image =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Copy everything
WORKDIR /App
COPY . ./

# Restore dependencies as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish -c Release -o out


# ===== Runtime image =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /App
COPY --from=build-env /App/out .

# Required for health checks
RUN apt update && apt install -y curl

ENTRYPOINT ["dotnet", "Api.dll"]
