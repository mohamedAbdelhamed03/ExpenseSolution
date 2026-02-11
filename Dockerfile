# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["ExpenseSolution.slnx", "./"]
COPY ["Expense.API/Expense.API.csproj", "Expense.API/"]
COPY ["Expense.Core/Expense.Core.csproj", "Expense.Core/"]
COPY ["Expense.Infrastructure/Expense.Infrastructure.csproj", "Expense.Infrastructure/"]
COPY ["Expense.UnitTests/Expense.UnitTests.csproj", "Expense.UnitTests/"]
COPY ["Expense.IntegrationTests/Expense.IntegrationTests.csproj", "Expense.IntegrationTests/"]

# Restore dependencies
RUN dotnet restore "Expense.API/Expense.API.csproj"

# Copy the remaining source code
COPY . .

# Build the API project
WORKDIR "/src/Expense.API"
RUN dotnet build "Expense.API.csproj" -c Release -o /app/build

# Publish the API project
FROM build AS publish
RUN dotnet publish "Expense.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Expense.API.dll"]
