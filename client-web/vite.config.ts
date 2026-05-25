import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          canvas: ["konva", "react-konva"],
          i18n: ["i18next", "react-i18next"],
          react: ["react", "react-dom", "react-router-dom"],
          validation: ["ajv"],
        },
      },
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
    },
  },
});
