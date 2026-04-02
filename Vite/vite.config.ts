import { defineConfig } from 'vite';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import babel from '@rolldown/plugin-babel';
import tailwind from '@tailwindcss/postcss'; // Исправленный импорт
import autoprefixer from 'autoprefixer';

export default defineConfig({
  plugins: [
    react(),
    babel({ presets: [reactCompilerPreset()] })
  ],
  css: {
    postcss: {
      plugins: [
        tailwind,
        autoprefixer
      ]
    }
  },
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
});
