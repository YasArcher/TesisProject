// tailwind.config.js
module.exports = {
    darkMode: ['class', '[data-theme="dark"]'],
    content: [
        "./**/*.razor",
        "./**/*.cshtml",
        "./**/*.html",
        "./**/*.js"
    ],
    theme: {
        extend: {
            colors: {
                brand: {
                    primary: "#7A1E19",   // header
                    surface: "#E3E2E1",   // bg de página
                    muted: "#6E6E6E",     // menú/footer
                    ink: "#1F2937",     // texto principal (slate-800 aprox)
                    inkSoft: "#475569",   // texto secundario
                    accent: "#2563EB",    // acciones (azul)
                    success: "#059669",
                    warning: "#D97706",
                    danger: "#DC2626",
                    card: "#FFFFFF"       // cards sólidas
                }
            },
            fontFamily: {
                display: ["Rubik", "Inter", "Segoe UI", "system-ui", "sans-serif"],
                body: ["Inter", "Segoe UI", "system-ui", "sans-serif"]
            },
            boxShadow: {
                soft: "0 6px 20px rgba(0,0,0,0.08)",
                ring: "0 0 0 3px rgba(37, 99, 235, 0.25)"
            },
            borderRadius: {
                xl2: "1rem"
            }
        }
    },
    plugins: []
}
