window.finanzasTheme = {
    initialize() {
        const isDark = document.documentElement.dataset.theme === "dark";
        this.apply(isDark);
        return isDark;
    },
    apply(isDark) {
        const theme = isDark ? "dark" : "light";
        document.documentElement.dataset.theme = theme;

        try {
            localStorage.setItem("finanzas-theme", theme);
        } catch {
        }
    }
};