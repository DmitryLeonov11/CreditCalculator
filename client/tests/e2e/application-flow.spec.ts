import { test, expect } from '@playwright/test'

test.use({ storageState: { cookies: [], origins: [] } })

test.describe('End-to-end: регистрация → заявка → решение', () => {
  const uniqueEmail = `e2e.${Date.now()}@example.by`
  const password = 'TestPass123'

  test('полный сценарий: регистрация, вход, анкета, заявка, скоринг', async ({
    page,
    request,
  }) => {
    // 1. Регистрация через API (минуя подтверждение почты в тестовом окружении)
    const registerResponse = await request.post('/api/v1/auth/register', {
      data: {
        email: uniqueEmail,
        password,
        personalDataConsent: true,
      },
    })
    expect(registerResponse.ok()).toBeTruthy()

    // 2. Вход через UI
    await page.goto('/login')
    await page.fill('#login-email', uniqueEmail)
    await page.fill('#login-password', password)
    await page.click('button[type="submit"]')
    await expect(page).toHaveURL('/')

    // 3. Переход к анкете и заполнение
    await page.click('nav text=Анкета')
    await page.click('button:has-text("Редактировать")')
    await page.fill('#profile-full-name', 'Иванов Иван Иванович')
    await page.fill('#profile-birth-date', '1990-05-15')
    await page.selectOption('#profile-gender', 'Male')
    await page.fill('#profile-monthly-income', '3000')
    await page.fill('#profile-employment-months', '36')
    await page.fill('#profile-existing-monthly-payments', '0')
    await page.fill('#profile-dependents', '0')
    await page.click('button:has-text("Сохранить анкету")')
    await expect(page.locator('.form-success')).toContainText(
      'Анкета сохранена',
    )

    // 4. Подача заявки
    await page.click('nav text=Новая заявка')
    await page.fill('#application-amount', '45000')
    await page.fill('#application-term', '60')
    await page.click('button.application-submit')
    await expect(page).toHaveURL('/applications')

    // 5. Проверка статуса заявки и результата скоринга
    await expect(
      page.locator(
        'text=UnderReview, text=На рассмотрении, text=Одобрено, text=Approved',
      ),
    ).toBeVisible()
    await expect(
      page.locator('text=Scoring, text=Скоринг, text=Балл, text=score'),
    ).toBeVisible()
  })
})
