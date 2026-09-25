# CreditCalculator

Кредитный калькулятор с поддержкой аннуитетных и дифференцированных платежей, эффективной процентной ставкой (ППС) и пересчётом графика при досрочном погашении.

## Быстрый старт

```bash
dotnet restore
dotnet run --project CreditCalculator.Api
```

API поднимется на `http://localhost:5288` (профиль `http`) или `https://localhost:7265` (профиль `https`, запускается по умолчанию в Visual Studio/`dotnet run`).

Через Docker Compose (только API):

```bash
docker compose up --build
```

API будет доступен на `http://localhost:8080`.

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

## Тесты

```bash
dotnet test
```
