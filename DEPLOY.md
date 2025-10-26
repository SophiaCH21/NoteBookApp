# 🚀 Деплой на Render

## Быстрый старт

### Шаг 1: Подготовка
```bash
# Установите зависимости backend
cd NoteManagerApi
dotnet restore

# Примените миграции локально (опционально)
dotnet ef database update
```

### Шаг 2: Deploy на Render

#### Вариант A: Использование render.yaml (Рекомендуется)

1. Зайдите на [Render Dashboard](https://dashboard.render.com/)
2. Нажмите "New +" → "Blueprint"
3. Подключите ваш GitHub репозиторий
4. Render автоматически создаст все сервисы из `render.yaml`

#### Вариант B: Ручной деплой

Следуйте детальным инструкциям в [DEPLOY_INSTRUCTIONS.md](./DEPLOY_INSTRUCTIONS.md)

### Шаг 3: Настройка переменных окружения

#### Backend
В Render Dashboard → ваш backend service → Environment:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
ConnectionStrings__DefaultConnection=<из PostgreSQL service>
Jwt__Key=<сгенерируйте ключ минимум 32 символа>
Jwt__Issuer=NoteManagerApi
Jwt__Audience=NoteManagerClient
FrontendUrl=https://<ваш-фронтенд>.onrender.com
```

#### Frontend
В Render Dashboard → ваш frontend service → Environment (только при повторной сборке):

```
VITE_API_URL=https://<ваш-бэкенд>.onrender.com/api
```

## 🔑 Генерация секретного ключа JWT

**Windows PowerShell:**
```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**Linux/Mac:**
```bash
openssl rand -base64 32
```

## 📝 После деплоя

1. Примените миграции (если нужно):
   - Миграции применяются автоматически при старте backend
   - Или выполните вручную через Shell в Render Dashboard

2. Проверьте работу:
   - Backend Swagger: `https://<ваш-бэкенд>.onrender.com/swagger`
   - Frontend: `https://<ваш-фронтенд>.onrender.com`

3. Обновите CORS в backend:
   - Добавьте URL вашего frontend в настройки

## 🛠️ Сборка проекта

### Локальная разработка
```bash
# Backend
cd NoteManagerApi
dotnet run

# Frontend  
cd NoteAppFrontend
npm install
npm run dev
```

### Production build
```bash
# Backend
cd NoteManagerApi
dotnet publish -c Release

# Frontend
cd NoteAppFrontend
npm install
npm run build
# Результат в папке dist/
```

## 💰 Тарифы

- PostgreSQL: **Free** (90MB)
- Backend: **Free** (auto-sleep после 15 мин бездействия)
- Frontend: **Free**

## ⚠️ Ограничения Free плана

- Backend засыпает после 15 минут бездействия
- Первый запрос после сна может занять 30-60 сек
- PostgreSQL ограничен 90MB

## 📚 Дополнительная документация

- [Подробная инструкция](./DEPLOY_INSTRUCTIONS.md)
- [Render Documentation](https://render.com/docs)

