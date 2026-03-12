# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release --no-build -o /app

# Runtime stage — use standard image (not alpine) for CUDA compatibility
# CUDA runtime libraries are provided by the Azure Container Apps GPU workload profile.
# The ONNX Runtime CUDA binaries come from the ElBruno.Text2Image.Cuda NuGet package.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8088
ENTRYPOINT ["dotnet", "HostedAgent.dll"]
