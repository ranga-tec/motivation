# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY POMS/POMS.sln ./POMS/
COPY POMS/src/Poms.Domain/Poms.Domain.csproj ./POMS/src/Poms.Domain/
COPY POMS/src/Poms.Application/Poms.Application.csproj ./POMS/src/Poms.Application/
COPY POMS/src/Poms.Infrastructure/Poms.Infrastructure.csproj ./POMS/src/Poms.Infrastructure/
COPY POMS/src/Poms.Reporting/Poms.Reporting.csproj ./POMS/src/Poms.Reporting/
COPY POMS/src/Poms.Web/Poms.Web.csproj ./POMS/src/Poms.Web/
COPY POMS/tests/Poms.Tests/Poms.Tests.csproj ./POMS/tests/Poms.Tests/

# Restore dependencies
RUN dotnet restore POMS/POMS.sln

# Copy everything else
COPY POMS/ ./POMS/

# Build and publish
WORKDIR /src/POMS/src/Poms.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Railway provides PORT environment variable at runtime
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Poms.Web.dll"]
