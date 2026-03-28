FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy only the project file first for better layer caching
COPY Switchly-2.0.WebApi/Switchly-2.0.WebApi.csproj ./
RUN dotnet restore "./Switchly-2.0.WebApi.csproj"

# Copy only this project folder (build context may be repo root)
COPY Switchly-2.0.WebApi/. ./

# Publish (no restore needed because it was done above)
RUN dotnet publish "./Switchly-2.0.WebApi.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080


COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Switchly-2.0.WebApi.dll"]