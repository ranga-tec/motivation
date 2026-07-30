(() => {
    "use strict";

    const storageKey = "poms-color-theme";
    const allowedThemes = new Set(["classic", "light", "dark"]);

    const readTheme = () => {
        try {
            const saved = window.localStorage.getItem(storageKey);
            return allowedThemes.has(saved) ? saved : "classic";
        } catch {
            return "classic";
        }
    };

    const applyTheme = (theme) => {
        const selectedTheme = allowedThemes.has(theme) ? theme : "classic";
        document.documentElement.dataset.pomsTheme = selectedTheme;
        document.documentElement.dataset.bsTheme = selectedTheme === "dark" ? "dark" : "light";
        document.documentElement.style.colorScheme = selectedTheme === "dark" ? "dark" : "light";
        return selectedTheme;
    };

    const initialTheme = applyTheme(readTheme());

    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("[data-theme-selector]").forEach((selector) => {
            selector.value = initialTheme;
            selector.addEventListener("change", () => {
                const selectedTheme = applyTheme(selector.value);
                try {
                    window.localStorage.setItem(storageKey, selectedTheme);
                } catch {
                    // The theme still applies for this page when storage is unavailable.
                }
                document.querySelectorAll("[data-theme-selector]").forEach((item) => {
                    item.value = selectedTheme;
                });
            });
        });
    });
})();
