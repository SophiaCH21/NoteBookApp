# Инструкция по деплою на Render

## 🚀 Быстрый деплой

### 1. Подготовка репозитория

Убедитесь, что все изменения закоммичены:
```bash
git add .
git commit -m "Add Render deployment configuration"
git push
```

### 2. Создание базы данных PostgreSQL на Render

1. Зайдите на [Render Dashboard](https://dashboard.render.com/)
2. Нажмите "New +" → "PostgreSQL"
3. Заполните настройки:
   - **Name**: `notemanager-db`
   - **Database**: `notemanagerdb` (или по умолчанию)
   - **User**: `notemanageruser` (или по умолчанию)
   - **Region**: Frankfurt (ближайший к вам)
   - **Plan**: Free
4. Нажмите "Create Database"
5. **Сохраните Connection String** - он понадобится позже!

### 3. Деплой Backend API

1. Нажмите "New +" → "Web Service"
2. Подключите ваш GitHub репозиторий
3. Заполните настройки:

**Основные настройки:**
- **Name**: `notemanager-api`
- **Region**: Frankfurt
- **Branch**: `main`
- **Root Directory**: `NoteManagerApi`

**Environment**: Docker

**Build Command**: (оставьте пустым)

**Start Command**: (оставьте пустым)

**Variables Environment**:
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
ConnectionStrings__DefaultConnection=<Connection String из PostgreSQL>
Jwt__Key=<сгенерируйте секретный ключ минимум 32 символа>
Jwt__Issuer=NoteManagerApi
Jwt__Audience=NoteManagerClient
FrontendUrl=https://<имя-вашего-фронтенда>.onrender.com
CORS__AllowedOrigins=["https://<имя-вашего-фронтенда>.onrender.com","http://localhost:5173"]
```

4. Нажмите "Create Web Service"
5. Дождитесь завершения деплоя (обычно 5-10 минут)

### 4. Деплой Frontend

1. Нажмите "New +" → "Static Site"
2. Подключите ваш GitHub репозиторий
3. Заполните настройки:

**Основные настройки:**
- **Name**: `notemanager-frontend`
- **Region**: Frankfurt
- **Branch**: `main`
- **Root Directory**: `NoteAppFrontend`

**Build Command**: 
```bash
npm install && npm run build
```

**Publish Directory**: `dist`

**Environment Variables**:
```
VITE_API_URL=https://<имя-вашего-бэкенда>.onrender.com/api
```

4. Нажмите "Create Static Site"
5. Дождитесь завершения деплоя

### 5. Обновите Frontend URL в Backend

После деплоя frontend вернитесь к настройкам Backend и обновите:

```
FrontendUrl=https://<реальное-имя-фронтенда>.onrender.com
CORS__AllowedOrigins=["https://<реальное-имя-фронтенда>.onrender.com"]
```

Сохраните и подождите автоматического передеплоя.

### 6. Применение миграций базы данных

После деплоя backend выполните миграции. В Render Dashboard:

1. Откройте ваш Backend Service
2. Перейдите на вкладку "Shell"
3. Выполните:
```bash
dotnet ef database update
```

Или добавьте в Dockerfile автоматическое применение миграций при старте.

## 📝 Примечания

### Миграции базы данных

Миграции НЕ применяются автоматически. Есть два варианта:

**Вариант 1: Ручное применение**
1. В Render Dashboard откройте Backend → Shell
2. Выполните: `dotnet ef database update`

**Вариант 2: Автоматическое применение**
Измените `Program.cs` чтобы добавить автоматическое применение миграций:

```csharp
// После app.Build()
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate(); // Автоматическое применение миграций
}
```

### Генерация JWT ключа

Для продакшн сгенерируйте безопасный ключ минимум 32 символа:

**Windows PowerShell:**
```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**Linux/Mac:**
```bash
openssl rand -base64 32
```

### Проверка деплоя

1. Backend должен быть доступен по: `https://<имя-бэкенда>.onrender.com/swagger`
2. Frontend должен быть доступен по: `https://<имя-фронтенда>.onrender.com`
3. Логи можно посмотреть в Render Dashboard → Logs

## 🐛 Решение проблем

### Backend не запускается
- Проверьте логи в Render Dashboard
- Убедитесь что все переменные окружения установлены
- Проверьте connection string для PostgreSQL

### CORS ошибки
- Проверьте что `FrontendUrl` в backend совпадает с реальным URL frontend
- Убедитесь что CORS политика включает ваш frontend URL

### Миграции не применяются
- Выполните вручную через Shell
- Или добавьте автоматическое применение в Program.cs

### Frontend не подключается к API
- Проверьте что `VITE_API_URL` установлен правильно
- Откройте DevTools → Network и проверьте запросы к API

## 💰 Стоимость

Все сервисы используют **Free план**:
- PostgreSQL: 90MB база данных
- Backend: Автоматически засыпает после 15 минут бездействия
- Frontend: Полностью бесплатный

**Ограничения Free плана:**
- Backend может "засыпать" при отсутствии запросов 15+ минут
- Первый запрос после сна может занять 30-60 секунд
- PostgreSQL имеет лимит 90MB

Если нужен более мощный план - оформите оплату в Render Dashboard.

