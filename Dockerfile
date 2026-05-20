# Use the official Microsoft ASP.NET Core runtime image as a base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official Microsoft .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and project files first to restore dependencies
COPY ["BelumiApi.sln", "./"]
COPY ["BelumiApi/Belumi.API/Belumi.API.csproj", "BelumiApi/Belumi.API/"]
COPY ["BelumiApi/Belumi.Application/Belumi.Application.csproj", "BelumiApi/Belumi.Application/"]
COPY ["BelumiApi/Belumi.Core/Belumi.Core.csproj", "BelumiApi/Belumi.Core/"]
COPY ["BelumiApi/Belumi.Infrastructure/Belumi.Infrastructure.csproj", "BelumiApi/Belumi.Infrastructure/"]
COPY ["BelumiApi/Belumi.Tests/Belumi.Tests.csproj", "BelumiApi/Belumi.Tests/"]

# Restore dependencies
RUN dotnet restore "BelumiApi.sln"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/BelumiApi/Belumi.API"
RUN dotnet build "Belumi.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Belumi.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Copy the published output to the runtime base image and set the entrypoint
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Ensure the wwwroot/uploads directory exists for avatars
RUN mkdir -p wwwroot/uploads

ENTRYPOINT ["dotnet", "Belumi.API.dll"]
