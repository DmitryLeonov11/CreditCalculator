# CreditCalculator

Кредитный калькулятор с поддержкой аннуитетных и дифференцированных платежей, эффективной процентной ставкой (ППС) и пересчётом графика при досрочном погашении. Пользователи регистрируются, входят по JWT и заполняют анкету; справочник кредитных продуктов хранится в PostgreSQL.

## Быстрый старт

Секреты (пароль БД и ключ подписи JWT) в репозиторий не попадают. Для Docker Compose они берутся из файла `.env`, для `dotnet run` — из User Secrets.

### Docker Compose (frontend + API + PostgreSQL)

```bash
cp .env.example .env   # подставить POSTGRES_PASSWORD и JWT_SIGNING_KEY
docker compose up --build
```

Фронтенд будет доступен на `http://localhost:3000`, API — на `http://localhost:8080`, PostgreSQL — на `localhost:5432`. Клиент обращается к API через Nginx внутри Compose. Если порты заняты, задайте `FRONTEND_PORT`, `API_PORT` и `DB_PORT` в `.env`.

В Development API разрешает браузерные запросы с локального Compose-фронтенда и Vite (`http://localhost:5173` и `http://127.0.0.1:5173`). Для Compose origin по умолчанию строится из `FRONTEND_PORT`; при публикации за другим доменом задайте в `.env` `FRONTEND_ORIGIN=https://frontend.example.com`. В production укажите реальные origin в `Cors:AllowedOrigins` (или в `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1` и т.д.); список должен содержать хотя бы один точный HTTP(S)-origin, иначе API не запустится. Origin — это только схема, хост и необязательный порт, без пути и завершающего `/`.

Вне Development API включает HSTS на один год и security-заголовки. TLS должен завершаться на Kestrel либо на reverse proxy, который перенаправляет HTTP на HTTPS. При TLS termination передайте API `X-Forwarded-Proto` только от доверенного proxy и задайте его IP в `ReverseProxy:KnownProxies` (например, `ReverseProxy__KnownProxies__0=10.0.0.10`); не доверяйте forwarded-заголовкам от произвольных клиентов. Сам reverse proxy также должен перенаправлять внешние HTTP-запросы на HTTPS.

### Локально через `dotnet run`

Нужна запущенная база, например из того же compose (`docker compose up -d db`). Затем:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=creditcalculator;Username=creditcalculator;Password=<пароль из .env>" --project CreditCalculator.Api
dotnet user-secrets set "Jwt:SigningKey" "<та же строка, что JWT_SIGNING_KEY>" --project CreditCalculator.Api
dotnet run --project CreditCalculator.Api
```

API поднимется на `http://localhost:5288` (профиль `http`) или `https://localhost:7265` (профиль `https`, запускается по умолчанию в Visual Studio/`dotnet run`). Без строки подключения, внешнего адреса `PublicUrl:BaseUrl` или с ключом подписи короче 32 символов приложение не стартует и сообщает, чего не хватает. Полный список настроек с плейсхолдерами — в `CreditCalculator.Api/appsettings.Example.json`.

В dev-режиме при старте применяются миграции и создаются начальные данные: кредитные продукты и тестовый сотрудник `employee@creditcalculator.local` с паролем `Employee123` (задаётся в секции `Seed` файла `appsettings.Development.json`). В продакшене миграции должны применяться отдельным шагом деплоя.

### Миграции

Миграции лежат в `CreditCalculator.Infrastructure/Persistence/Migrations`. Для `dotnet ef` Api запускать не нужно: контекст создаётся фабрикой времени разработки.

```bash
dotnet ef migrations add <Имя> --project CreditCalculator.Infrastructure --output-dir Persistence/Migrations
```

## Документация API

В dev-режиме (`ASPNETCORE_ENVIRONMENT=Development`) поднимается интерактивная документация Scalar по OpenAPI-схеме:

- Scalar UI: `http://localhost:5288/scalar`
- OpenAPI JSON: `http://localhost:5288/openapi/v1.json`

## Эндпоинты калькулятора

Базовый путь: `/api/v1/calculator`.

### `POST /api/v1/calculator/schedule` — построение графика платежей

Тело запроса:

```json
{
  "amount": 20000,
  "termMonths": 24,
  "annualRatePercent": 18,
  "paymentType": "Annuity",
  "firstPaymentDate": "2026-01-01"
}
```

- `paymentType` — `"Annuity"` или `"Differentiated"`.
- Ограничения: сумма 500–300 000 BYN, срок 1–240 месяцев, ставка 0,1–60% годовых.

Пример запроса через `curl`:

```bash
curl -X POST http://localhost:5288/api/v1/calculator/schedule \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 20000,
    "termMonths": 24,
    "annualRatePercent": 18,
    "paymentType": "Annuity",
    "firstPaymentDate": "2026-01-01"
  }'
```

Ответ:

```json
{
  "payments": [
    {
      "number": 1,
      "date": "2026-01-01",
      "payment": 998.48,
      "interestPart": 300.00,
      "principalPart": 698.48,
      "remainingBalance": 19301.52
    }
  ],
  "monthlyPayment": 998.48,
  "totalPaid": 23963.59,
  "overpayment": 3963.59,
  "effectiveRate": 19.56
}
```

### `POST /api/v1/calculator/early-repayment` — сравнение графика до и после досрочного погашения

Тело запроса:

```json
{
  "amount": 20000,
  "termMonths": 24,
  "annualRatePercent": 18,
  "firstPaymentDate": "2026-01-01",
  "mode": "ReduceTerm",
  "earlyRepayments": [
    { "month": 6, "amount": 3000 }
  ]
}
```

- `mode` — `"ReduceTerm"` (сокращение срока, платёж не меняется) или `"ReducePayment"` (пересчёт платежа при том же сроке).
- `earlyRepayments` — список досрочных погашений (`month` от 1 до `termMonths`, `amount` больше нуля).

Ответ содержит два графика — `original` (без досрочных погашений) и `withEarlyRepayments` (пересчитанный) — в том же формате, что и у `/schedule`.

## Регистрация и вход

Базовый путь: `/api/v1/auth`. На `register` и `login` действует ограничение частоты запросов: по умолчанию 5 попыток в минуту с одного IP (секция `RateLimiting:Auth`). Сверх лимита API отвечает `429`.

| Метод | Путь | Что делает |
|---|---|---|
| `POST` | `/register` | Регистрация клиента. Ответ `202` без тела, ссылка подтверждения пишется в лог — почтовый сервер пока не настроен |
| `GET` | `/confirm-email?token=...` | Подтверждение email по ссылке из письма (ссылка действует сутки). Ответ `204` |
| `POST` | `/login` | Вход. До подтверждения email — `403`, при неверной паре email/пароль — `401` |
| `POST` | `/refresh` | Новая пара токенов по refresh-токену. Старый refresh-токен при этом отзывается |

Ответ на `register` не зависит от того, занят ли email, — иначе через регистрацию можно было бы проверять, у кого есть аккаунт. Если адрес уже зарегистрирован, но не подтверждён, на него повторно уходит ссылка подтверждения; если подтверждён — письмо не отправляется. Поэтому после регистрации клиент показывает всем одну подсказку: «Мы отправили ссылку для подтверждения на ваш email. Если письма нет, а аккаунт с этим адресом у вас уже есть, просто войдите». Ссылка в письме строится от `PublicUrl:BaseUrl`, а не от заголовка `Host` запроса.

```jsonc
// POST /api/v1/auth/register
{ "email": "client@example.com", "password": "Password123", "personalDataConsent": true }

// POST /api/v1/auth/login → 200
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAt": "2026-09-27T10:15:00+00:00",
  "refreshToken": "q3Jp0v...",
  "refreshTokenExpiresAt": "2026-10-04T10:00:00+00:00"
}

// POST /api/v1/auth/refresh
{ "refreshToken": "q3Jp0v..." }
```

Access-токен живёт 15 минут, refresh-токен — 7 дней. Защищённые эндпоинты ждут заголовок `Authorization: Bearer <accessToken>`. Права проверяются политиками `ClientOnly` и `EmployeeOnly` по роли из токена.

## Анкета клиента

`GET /api/v1/profile` и `PUT /api/v1/profile` — только для роли `Client`. `GET` отвечает `404`, пока анкета не заполнена.

```jsonc
// PUT /api/v1/profile
{
  "fullName": "Иванов Иван Иванович",
  "birthDate": "1990-05-15",
  "monthlyIncome": 2500.50,
  "employmentMonths": 36,
  "existingMonthlyPayments": 300,
  "dependents": 1
}
```

Ограничения: возраст от 18 до 100 лет, доход и текущие платежи не отрицательные, стаж 0–600 месяцев, иждивенцев 0–20.

## Справочник кредитных продуктов

`GET /api/v1/products` — активные продукты: назначение (`Consumer`, `Auto`, `Mortgage`), границы суммы и срока, базовая ставка. Авторизация не нужна.

## Тесты

```bash
dotnet test
```

- `tests/CreditCalculator.Calculations.Tests` — расчёты графиков, ППС и досрочного погашения.
- `tests/CreditCalculator.Api.IntegrationTests` — API целиком через `WebApplicationFactory` на настоящем PostgreSQL в Testcontainers: регистрация, подтверждение email, вход, обновление токенов, анкета, справочник продуктов, rate limiting. Для этих тестов нужен запущенный Docker.

## Клиентские тесты

```bash
cd client
npm run test
```

Запускает Vitest с React Testing Library для калькулятора (форма, валидация, пересчёт графика, досрочное погашение). E2E Playwright-тесты — `npm run test:e2e`.

## Стек

- **Бэкенд**: ASP.NET Core Web API (.NET 10), PostgreSQL 17, EF Core, JWT (access/refresh), Scalar/OpenAPI
- **Клиент**: React 18, TypeScript, Vite, Ant Design, TanStack Query, React Hook Form + Zod, Recharts, Vitest + Playwright
- **CI/CD**: GitHub Actions (.NET, Node.js, Docker build/push в GHCR)
- **Деплой**: Docker Compose (API + frontend + PostgreSQL)

## Деплой

### VPS с Docker Compose (основной вариант)

1. Клонируйте репозиторий на сервер.
2. Скопируйте `.env.example` в `.env` и укажите `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY` (минимум 32 символа), а при необходимости `FRONTEND_ORIGIN`, `API_PORT`, `DB_PORT`.
3. Запустите: `docker compose up --build -d`.
4. API будет доступен на порту `API_PORT` (по умолчанию 8080), фронтенд — на `FRONTEND_PORT` (по умолчанию 3000).
5. Для production-запуска настройте reverse proxy (Nginx/Traefik) с TLS termination и перенаправлением HTTP → HTTPS. Передайте `X-Forwarded-Proto` от доверенного proxy и укажите его IP в `ReverseProxy:KnownProxies`.


## Мониторинг

API предоставляет:

- `GET /health` — health checks (PostgreSQL + Redis), см. `docker-compose.yml`.
- `GET /metrics` — метрики в формате Prometheus (OpenTelemetry Prometheus exporter).

В `docker-compose.yml` уже поднимаются `prometheus` (порт 9090) и `grafana` (порт 3001). Prometheus сконфигурирован в `monitoring/prometheus.yml` и скрапит эндпоинт `/metrics` API. Grafana доступна с учётными данными из `.env` (`GRAFANA_ADMIN_USER` / `GRAFANA_ADMIN_PASSWORD`, по умолчанию `admin`/`admin`).

## Нагрузочное тестирование (k6)

Нагрузочный тест лежит в `k6/load-test.js`. Проверяет эндпоинты справочника продуктов, расчёта графика платежей и досрочного погашения.

```bash
# Запуск локально (API должен быть доступен)
docker run --rm -i grafana/k6 run - < k6/load-test.js

# Или с переменной BASE_URL, если API на другом хосте/порту
BASE_URL=http://localhost:8080 docker run --rm -i -e BASE_URL grafana/k6 run - < k6/load-test.js
```

Тест разгоняется до 20 виртуальных пользователей и удерживает нагрузку 1 минуту. Пороги: 95-й перцентил времени отклика < 1 секунды, успешность проверок > 99 %.

## Тестовые аккаунты

При первом запуске (development) создаются начальные данные:

| Роль | Email | Пароль |
|------|-------|--------|
| Сотрудник | `employee@creditcalculator.local` | `Employee123` |
| Клиент | создаётся при регистрации через `/api/v1/auth/register` | задаётся при регистрации |

Тестовый сотрудник имеет доступ к очереди заявок, одобрению и отклонению. Клиент регистрируется самостоятельно и подтверждает email по ссылке из лога API.

## Персональные данные

Анкета клиента содержит персональные данные (ФИО, дата рождения, доход, стаж, иждивенцы). Они хранятся в PostgreSQL и используются только для скоринга и расчёта кредитных продуктов. Email используется для подтверждения регистрации и уведомлений о статусе заявки. Пароли хранятся в виде хэшей (bcrypt через `PasswordHasher<T>`).
