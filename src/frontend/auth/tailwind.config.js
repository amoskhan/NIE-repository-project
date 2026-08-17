/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
    "../packages/ui/src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        primary: {
          50: "var(--theme-color-brand-50)",
          100: "var(--theme-color-brand-100)",
          200: "var(--theme-color-brand-200)",
          300: "var(--theme-color-brand-300)",
          400: "var(--theme-color-brand-400)",
          500: "var(--theme-color-brand-500)",
          600: "var(--theme-color-brand-600)",
          700: "var(--theme-color-brand-700)",
          800: "var(--theme-color-brand-800)",
          900: "var(--theme-color-brand-900)",
          950: "var(--theme-color-brand-950)",
        },
        secondary: {
          50: "var(--theme-color-neutral-50)",
          100: "var(--theme-color-neutral-100)",
          200: "var(--theme-color-neutral-200)",
          300: "var(--theme-color-neutral-300)",
          400: "var(--theme-color-neutral-400)",
          500: "var(--theme-color-neutral-500)",
          600: "var(--theme-color-neutral-600)",
          700: "var(--theme-color-neutral-700)",
          800: "var(--theme-color-neutral-800)",
          900: "var(--theme-color-neutral-900)",
          950: "var(--theme-color-neutral-950)",
        },
        accent: "var(--color-accent)",
        "accent-light": "var(--color-accent-light)",
        "background-light": "var(--color-bg-light)",
        "background-dark": "var(--color-bg-dark)",
        surface: "var(--color-surface)",
        "surface-alt": "var(--color-surface-alt)",
      },
      fontFamily: {
        display: ["var(--theme-font-display)", "Lexend", "sans-serif"],
        body: ["var(--theme-font-body)", "Lexend", "sans-serif"],
      },
      borderRadius: {
        lg: "var(--theme-radius-lg)",
        xl: "var(--theme-radius-xl)",
      },
      boxShadow: {
        soft: "var(--theme-shadow-soft)",
      },
    },
  },
  plugins: [],
};
