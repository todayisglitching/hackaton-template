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
  }
});
