# syntax=docker/dockerfile:1

# --- Frontend (Vite React SPA) ---
FROM node:22-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# --- Backend (.NET API) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/Home.Api/Home.Api.csproj backend/Home.Api/
RUN dotnet restore backend/Home.Api/Home.Api.csproj
COPY backend/Home.Api/ backend/Home.Api/
COPY --from=frontend /src/frontend/dist/ backend/Home.Api/wwwroot/
RUN dotnet publish backend/Home.Api/Home.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime SPA (API serves React) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /app/uploads
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=backend /app/publish .
VOLUME ["/app/uploads"]
ENTRYPOINT ["dotnet", "Home.Api.dll"]
