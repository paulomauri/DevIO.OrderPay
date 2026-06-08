# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Apps/DevIO.OrderPay.WebApi/DevIO.OrderPay.WebApi.csproj", "src/Apps/DevIO.OrderPay.WebApi/"]
COPY ["src/Apps/DevIO.OrderPay.Infra/DevIO.OrderPay.Infra.csproj", "src/Apps/DevIO.OrderPay.Infra/"]
COPY ["src/Apps/DevIO.OrderPay.Core/DevIO.OrderPay.Core.csproj", "src/Apps/DevIO.OrderPay.Core/"]
COPY ["src/Contexts/DevIO.OrderPay.Customer.Application/DevIO.OrderPay.Customer.Application.csproj", "src/Contexts/DevIO.OrderPay.Customer.Application/"]
COPY ["src/Contexts/DevIO.OrderPay.Customer/DevIO.OrderPay.Customer.csproj", "src/Contexts/DevIO.OrderPay.Customer/"]
RUN dotnet restore "./src/Apps/DevIO.OrderPay.WebApi/DevIO.OrderPay.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Apps/DevIO.OrderPay.WebApi"
RUN dotnet build "./DevIO.OrderPay.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DevIO.OrderPay.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevIO.OrderPay.WebApi.dll"]