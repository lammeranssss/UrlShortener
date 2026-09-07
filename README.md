# Enterprise High-Performance URL Shortener

Высоконагруженный, отказоустойчивый сервис сокращения ссылок, построенный на базе **.NET 9 Minimal API** и **React SPA (Vite + TypeScript)** с использованием дистролесс-контейнеризации и двухэтапного кэширования.

---

## Технологический стек

* **Backend**: .NET 9 (C# 12), Minimal API, Entity Framework Core 9, HybridCache, System.Threading.Channels.
* **Frontend**: React 18, TypeScript, Vite, TanStack Query v5, Tailwind CSS.
* **Database**: MariaDB 11.4 (InnoDB).
* **Infrastructure**: Nginx 1.27 (Reverse Proxy), Docker (Multi-stage Chiseled runtime), Docker Compose.
* **Testing**: xUnit, Testcontainers (.NET), FluentAssertions.
* **CI/CD**: GitHub Actions.

---

### Ключевые архитектурные особенности:
1. **Hot Path O(1) Redirect**: Запросы сокращенных кодов обслуживаются через `HybridCache`. При промахе кэша выполняется выборка из MariaDB с `AsNoTracking()`.
2. **High-load Click Aggregator**: Учет кликов не блокирует поток перенаправления. Коды асинхронно сбрасываются в `Channel<string>` и пакетами сохраняются воркером `ClickAggregatorWorker`.
3. **Collision Resilience**: Генерация 7-значных Base62-кодов защищена криптографическим генератором случайных чисел с автоматическим перехватом MySQL `ER_DUP_ENTRY` (Error 1062).
4. **Zero-Downtime Migration Runner**: DDL-миграции схемы БД выполняются отдельным сервисом до старта API, исключая блокировки таблиц и Race Conditions.

---

## Быстрый запуск (One-Click Deployment)

Для развертывания полного защищенного продуктового стека необходим только **Docker** и **Docker Compose**.

```bash
# 1. Клонируйте репозиторий
git clone [git clone https://github.com/lammeransssss/UrlShortener.git](git clone https://github.com/lammeransssss/UrlShortener.git)
cd UrlShortener
```

# 2. Запустите продуктовый стек в один клик

```bash
docker compose -f docker-compose.yml up --build -d
```

Приложение станет доступно по адресу: **`http://localhost:8080`**.

---

## Локальная разработка

### Требования

* .NET 9 SDK
* Node.js 22+ & npm
* Docker Desktop

### 1. Запуск инфраструктуры базы данных

```bash
docker compose up -d

```

### 2. Запуск REST API

```bash
dotnet run --project UrlShortener/UrlShortener.csproj --urls "http://localhost:5200"

```

### 3. Запуск React SPA

```bash
cd UrlShortener.Client
npm install
npm run dev

```

Фронтенд запустится по адресу `http://localhost:5173`.

---

## Тестирование

Интеграционные тесты используют **Testcontainers** для автоматического подъема изолированного контейнера MariaDB 11.4 на время прогона.

```bash
# Запуск полного тестового сюита
dotnet test UrlShortener.slnx

# Проверка типов Frontend
cd UrlShortener.Client && npx tsc -p tsconfig.app.json --noEmit

```

---

## Безопасность и Hardening

* **Principle of Least Privilege (PoLP)**: Бэкенд API взаимодействует с СУБД через ограниченную учетную запись `shortener_app` без прав DDL-изменений.
* **Non-Root Execution**: Контейнер API собран на базе образа `dotnet/aspnet:9.0-noble-chiseled` и запускается от пользователя `$APP_UID` (1654).
* **OWASP Headers & Rate Limits**: Nginx заблокирован от Clickjacking (`X-Frame-Options: DENY`), MIME-sniffing (`X-Content-Type-Options: nosniff`) и ограничивает размер тела запроса до `2MB`.
