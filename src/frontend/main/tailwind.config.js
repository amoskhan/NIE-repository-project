import forms from "@tailwindcss/forms";

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
        sidebar: "var(--color-sidebar)",
        "sidebar-active": "var(--color-sidebar-active)",
        text: "var(--color-text)",
        "text-muted": "var(--color-text-muted)",
        border: "var(--color-border)",
        success: "var(--theme-color-success-solid)",
        warning: "var(--theme-color-warning-solid)",
        danger: "var(--theme-color-danger-solid)",
        info: "var(--theme-color-info-solid)",
      },
      fontFamily: {
        display: ["var(--theme-font-display)", "Lexend", "sans-serif"],
        body: ["var(--theme-font-body)", "Lexend", "sans-serif"],
      },
      borderRadius: {
        DEFAULT: "var(--theme-radius-sm)",
        md: "var(--theme-radius-sm)",
        lg: "var(--theme-radius-md)",
        xl: "var(--theme-radius-lg)",
        "2xl": "var(--theme-radius-lg)",
        full: "9999px",
      },
      boxShadow: {
        soft: "var(--theme-shadow-soft)",
        glass: "var(--theme-shadow-card)",
        "inner-soft": "var(--theme-shadow-float)",
      },
      backgroundImage: {
        "glass-gradient":
          "linear-gradient(120deg, rgba(255,255,255,0.6), rgba(255,255,255,0.2))",
        "dark-glass-gradient":
          "linear-gradient(120deg, rgba(17,24,39,0.6), rgba(17,24,39,0.2))",
      },
      backdropBlur: {
        glass: "10px",
      },
      width: {
        "1/7": "14.2857143%",
      },
    },
  },
  plugins: [forms],
};
