import { defineConfig } from 'vite'
import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import babel from '@rolldown/plugin-babel'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // Оставляем ваш сетап с компилятором
    babel({ presets: [reactCompilerPreset()] })
  ],
  server: {
    proxy: {
      // Все запросы к /todos будут уходить на ASP.NET
      '/api': {
        target: 'http://localhost:5000', // Адрес вашего бэкенда
        changeOrigin: true,
        secure: false,
        ws: true,
        // МАГИЯ ЗДЕСЬ:
        // Эта функция берет путь "/api/todos" и превращает его в "/todos"
        // перед отправкой на сервер ASP.NET
        //rewrite: (path) => path.replace(/^\/api/, '')
      }
    }
  }
})
