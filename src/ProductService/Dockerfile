FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003
EXPOSE 50003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["src/ProductService/ProductService.csproj", "src/ProductService/"]
RUN dotnet restore "src/ProductService/ProductService.csproj"
COPY . .
WORKDIR "/src/src/ProductService"
RUN dotnet build "ProductService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish "ProductService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll", "--urls", "http://0.0.0.0:5003"]

