import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

export const requests = new Counter('http_requests_total');
export const responseTime = new Trend('http_response_duration_ms');

export const options = {
  stages: [
    { duration: '30s', target: 20 },   // разгон до 20 VU
    { duration: '1m', target: 20 },    // удержание 20 VU
    { duration: '30s', target: 0 },    // спад
  ],
  thresholds: {
    http_requests_total: ['count > 0'],
    http_response_duration_ms: ['p(95) < 1000'],
    checks: ['rate > 0.99'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

const schedulePayload = {
  loanAmount: 50000,
  termMonths: 24,
  annualRate: 12.5,
  paymentType: 'Annuity',
};

const earlyRepaymentPayload = {
  repayments: [
    { date: '2027-02-01', amount: 5000, mode: 'ReduceTerm' },
  ],
};

export default function () {
  // GET /api/v1/products — справочник продуктов (публичный)
  let res = http.get(`${BASE_URL}/api/v1/products`);
  requests.add(1);
  responseTime.add(res.timings.duration);
  check(res, {
    'products status 200': (r) => r.status === 200,
    'products has body': (r) => r.body.length > 0,
  });

  // POST /api/v1/calculator/schedule — расчёт графика
  res = http.post(`${BASE_URL}/api/v1/calculator/schedule`, JSON.stringify(schedulePayload), {
    headers: { 'Content-Type': 'application/json' },
  });
  requests.add(1);
  responseTime.add(res.timings.duration);
  check(res, {
    'schedule status 200': (r) => r.status === 200,
    'schedule has payments': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body.payments) && body.payments.length > 0;
      } catch (e) {
        return false;
      }
    },
  });

  // POST /api/v1/calculator/early-repayment — досрочное погашение
  res = http.post(`${BASE_URL}/api/v1/calculator/early-repayment`, JSON.stringify(earlyRepaymentPayload), {
    headers: { 'Content-Type': 'application/json' },
  });
  requests.add(1);
  responseTime.add(res.timings.duration);
  check(res, {
    'early-repayment status 200': (r) => r.status === 200,
  });

  sleep(1);
}
