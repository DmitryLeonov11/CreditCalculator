import React from 'react'
import ReactDOM from 'react-dom/client'
import { App as AntApp, ConfigProvider } from 'antd'
import ruRU from 'antd/locale/ru_RU'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import './index.css'
import { queryClient } from './lib/queryClient'
import { AuthProvider } from './features/auth/AuthProvider'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <ConfigProvider
            locale={ruRU}
            theme={{
              token: {
                colorPrimary: '#285d43',
                colorSuccess: '#347255',
                colorError: '#a23f38',
                colorText: '#202923',
                colorTextSecondary: '#647068',
                colorBorder: '#e1e7e0',
                borderRadius: 8,
                fontFamily: "'Aptos', 'Segoe UI', sans-serif",
                controlHeight: 42,
              },
            }}
          >
            <AntApp>
              <App />
            </AntApp>
          </ConfigProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>,
)
