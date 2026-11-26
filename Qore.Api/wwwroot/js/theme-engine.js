async function loadActiveTheme() {
    try {
        const response = await fetch('/api/themes/active', {
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
        });
        if (!response.ok) {
            return null;
        }

        const theme = await response.json();
        applyTheme(theme);
        return theme;
    } catch (error) {
        return null;
    }
}

function applyTheme(themeObject) {
    if (!themeObject) {
        return;
    }

    const root = document.documentElement;
    if (themeObject.primaryColor) {
        root.style.setProperty('--primary-color', themeObject.primaryColor);
    }
    if (themeObject.secondaryColor) {
        root.style.setProperty('--secondary-color', themeObject.secondaryColor);
    }
    if (themeObject.accentColor) {
        root.style.setProperty('--accent-color', themeObject.accentColor);
    }
    if (themeObject.backgroundColor) {
        root.style.setProperty('--background-color', themeObject.backgroundColor);
    }
    if (themeObject.textColor) {
        root.style.setProperty('--text-color', themeObject.textColor);
    }
}

window.applyTheme = applyTheme;
window.loadActiveTheme = loadActiveTheme;

if (typeof module !== 'undefined') {
    module.exports = { applyTheme, loadActiveTheme };
}
