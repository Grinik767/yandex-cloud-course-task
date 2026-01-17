FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["YandexCloudCourseTask.API.csproj", "./"]

RUN dotnet restore "YandexCloudCourseTask.API.csproj"

COPY . .

RUN dotnet publish "YandexCloudCourseTask.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT 8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "YandexCloudCourseTask.API.dll"]