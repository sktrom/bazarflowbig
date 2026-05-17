/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        primary: '#1d4ed8', // Example primary color
        secondary: '#64748b',
        success: '#22c55e',
        danger: '#ef4444',
        warning: '#f59e0b',
        info: '#3b82f6',
        surface: '#ffffff',
        background: '#f8fafc',
      },
      fontFamily: {
        sans: ['Tajawal', 'sans-serif'], // Arabic-friendly font
      }
    },
  },
  plugins: [],
}
