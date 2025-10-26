// API URL настраивается через переменные окружения
// В development: использует прокси через vite.config.ts
// В production на Render: использует абсолютный URL бэкенда
export const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';