FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Switchly-2.0.WebApi/Switchly-2.0.WebApi.csproj", "Switchly-2.0.WebApi/"]
RUN dotnet restore "Switchly-2.0.WebApi/Switchly-2.0.WebApi.csproj"
COPY . .
WORKDIR "/src/Switchly-2.0.WebApi"
RUN dotnet build "Switchly-2.0.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Switchly-2.0.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Switchly-2.0.WebApi.dll"]
