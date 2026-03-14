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

# Create storage directory for file uploads
RUN mkdir -p /app/storage && chmod 777 /app/storage

# Railway provides PORT environment variable at runtime
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=5000

# Use shell form to expand PORT variable
CMD ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet Poms.Web.dll
