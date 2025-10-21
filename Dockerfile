# 基于 .NET 8 的 AIReview.API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AIReview.API/AIReview.API.csproj", "AIReview.API/"]
RUN dotnet restore "AIReview.API/AIReview.API.csproj"
COPY . .
WORKDIR "/src/AIReview.API"
RUN dotnet publish "AIReview.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "AIReview.API.dll"]
