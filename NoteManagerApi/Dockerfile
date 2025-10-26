# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["NoteManagerApi.csproj", "./"]
RUN dotnet restore "NoteManagerApi.csproj"

# Копируем все файлы и собираем проект
COPY . .
RUN dotnet build "NoteManagerApi.csproj" -c Release -o /app/build

# Публикуем проект
RUN dotnet publish "NoteManagerApi.csproj" -c Release -o /app/publish

# Используем runtime образ для финального образа
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копируем опубликованные файлы
COPY --from=build /app/publish .

# Устанавливаем переменную окружения
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "NoteManagerApi.dll"]

