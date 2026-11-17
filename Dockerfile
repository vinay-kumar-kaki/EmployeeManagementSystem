# syntax=docker/dockerfile:1

# ---------- Stage 1: build the Angular SPA ----------
FROM node:20-alpine AS frontend
WORKDIR /frontend

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

# ---------- Stage 2: publish the .NET API ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src

COPY backend/EmployeeManagementSystem.API/EmployeeManagementSystem.API.csproj backend/EmployeeManagementSystem.API/
RUN dotnet restore backend/EmployeeManagementSystem.API/EmployeeManagementSystem.API.csproj

COPY backend/EmployeeManagementSystem.API/ backend/EmployeeManagementSystem.API/
RUN dotnet publish backend/EmployeeManagementSystem.API/EmployeeManagementSystem.API.csproj \
    -c Release -o /app/publish --no-restore

# ---------- Stage 3: single runtime image serving API + SPA ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=backend /app/publish ./
COPY --from=frontend /frontend/dist/employee-management-frontend/browser/ /app/wwwroot/

ENTRYPOINT ["dotnet", "EmployeeManagementSystem.API.dll"]
