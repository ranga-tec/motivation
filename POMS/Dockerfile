# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY POMS.sln ./
COPY src/Poms.Domain/Poms.Domain.csproj ./src/Poms.Domain/
COPY src/Poms.Application/Poms.Application.csproj ./src/Poms.Application/
COPY src/Poms.Infrastructure/Poms.Infrastructure.csproj ./src/Poms.Infrastructure/
COPY src/Poms.Reporting/Poms.Reporting.csproj ./src/Poms.Reporting/
COPY src/Poms.Web/Poms.Web.csproj ./src/Poms.Web/
COPY tests/Poms.Tests/Poms.Tests.csproj ./tests/Poms.Tests/

# Restore dependencies
RUN dotnet restore POMS.sln

# Copy everything else
COPY . ./

# Build and publish
WORKDIR /src/src/Poms.Web
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Railway provides PORT environment variable at runtime
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Poms.Web.dll"]
