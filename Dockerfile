FROM public.ecr.aws/docker/library/node:22-bookworm-slim AS frontend-build
WORKDIR /workspace
ENV npm_config_cache=/tmp/npm-cache
COPY frontend/package*.json frontend/
RUN cd frontend && npm ci
COPY frontend/ frontend/
RUN mkdir -p /workspace/backend/src/BalonmanoBunol.Api/wwwroot
WORKDIR /workspace/frontend
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /workspace
COPY backend/src/BalonmanoBunol.Domain/BalonmanoBunol.Domain.csproj backend/src/BalonmanoBunol.Domain/
COPY backend/src/BalonmanoBunol.Application/BalonmanoBunol.Application.csproj backend/src/BalonmanoBunol.Application/
COPY backend/src/BalonmanoBunol.Infrastructure/BalonmanoBunol.Infrastructure.csproj backend/src/BalonmanoBunol.Infrastructure/
COPY backend/src/BalonmanoBunol.Api/BalonmanoBunol.Api.csproj backend/src/BalonmanoBunol.Api/
COPY backend/src/BalonmanoBunol.Domain/ backend/src/BalonmanoBunol.Domain/
COPY backend/src/BalonmanoBunol.Application/ backend/src/BalonmanoBunol.Application/
COPY backend/src/BalonmanoBunol.Infrastructure/ backend/src/BalonmanoBunol.Infrastructure/
RUN dotnet restore backend/src/BalonmanoBunol.Api/BalonmanoBunol.Api.csproj
COPY backend/src/ backend/src/
RUN dotnet publish backend/src/BalonmanoBunol.Api/BalonmanoBunol.Api.csproj --configuration Release --no-restore --output /app/publish
COPY --from=frontend-build /workspace/backend/src/BalonmanoBunol.Api/wwwroot/ /app/publish/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:10000 \
    ASPNETCORE_ENVIRONMENT=Production \
    Frontend__WebRoot=/app/wwwroot \
    DOTNET_EnableDiagnostics=0
COPY --from=backend-build /app/publish ./
EXPOSE 10000
ENTRYPOINT ["dotnet", "BalonmanoBunol.Api.dll"]
