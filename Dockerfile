# syntax=docker/dockerfile:1.7

# ---- Build stage ---------------------------------------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy csproj files first to leverage Docker layer caching for dotnet restore
COPY app/Bookstore.Common/Bookstore.Common.csproj app/Bookstore.Common/
COPY app/Bookstore.Domain/Bookstore.Domain.csproj app/Bookstore.Domain/
COPY app/Bookstore.Data/Bookstore.Data.csproj     app/Bookstore.Data/
COPY app/Bookstore.Web/Bookstore.Web.csproj       app/Bookstore.Web/
RUN dotnet restore app/Bookstore.Web/Bookstore.Web.csproj -a $TARGETARCH

# Copy the rest of the sources and publish a self-contained output
COPY app/Bookstore.Common app/Bookstore.Common
COPY app/Bookstore.Domain app/Bookstore.Domain
COPY app/Bookstore.Data   app/Bookstore.Data
COPY app/Bookstore.Web    app/Bookstore.Web
RUN dotnet publish app/Bookstore.Web/Bookstore.Web.csproj \
        -c Release \
        -a $TARGETARCH \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ---- Runtime stage -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published artifacts and the web content directory used by LocalFileService
COPY --from=build /app/publish .
COPY app/Bookstore.Web/Content ./Content

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

# Run as non-root user provided by the base image
USER app

ENTRYPOINT ["dotnet", "Bookstore.Web.dll"]
