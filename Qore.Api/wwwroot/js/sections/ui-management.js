(function() {
const uiSection = document.querySelector('[data-section-content="ui"]');

let themes = [];
let activeThemeId = null;

const t = (key, fallback) => window.t ? window.t(key, fallback) : fallback;

const colorFields = [
    { key: 'primaryColor', labelKey: 'ui.primaryColor', fallback: 'Primary' },
    { key: 'secondaryColor', labelKey: 'ui.secondaryColor', fallback: 'Secondary' },
    { key: 'accentColor', labelKey: 'ui.accentColor', fallback: 'Accent' },
    { key: 'backgroundColor', labelKey: 'ui.backgroundColor', fallback: 'Background' },
    { key: 'textColor', labelKey: 'ui.textColor', fallback: 'Text' }
];

function hexOrDefault(value, fallback) {
    return value || fallback;
}

function isProtectedTheme(theme) {
    if (!theme) return false;
    const name = String(theme.name || '').trim().toLowerCase();
    return Boolean(
        theme.isDefault === true ||
        theme.IsDefault === true ||
        theme.isDefault === 1 ||
        theme.IsDefault === 1 ||
        name === 'qore dark blue' ||
        name.includes('default')
    );
}

function createThemeModal() {
    if (document.getElementById('theme-modal')) return;

    const modal = document.createElement('div');
    modal.id = 'theme-modal';
    modal.className = 'modal hidden';

    modal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header">
                <h3 id="theme-modal-title">${t('ui.addTheme', 'Add Theme')}</h3>
                <button class="icon-button" id="close-theme-modal">×</button>
            </div>
            <form id="theme-form" class="modal-form">
                <label class="modal-field">
                    <span>${t('ui.themeName', 'Name')}</span>
                    <input id="theme-name" required />
                </label>
                <div class="field-grid">
                    ${colorFields.map(f => `
                        <label class="modal-field">
                            <span>${t(f.labelKey, f.fallback)}</span>
                            <input type="color" id="${f.key}" value="#ffffff" />
                        </label>
                    `).join('')}
                </div>
                <div class="modal-field">
                    <span>${t('ui.livePreview', 'Live Preview')}</span>
                    <div id="theme-live-preview" class="theme-live-preview"></div>
                </div>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-theme" type="button">${t('common.cancel', 'Cancel')}</button>
                    <button class="primary-button" type="submit">${t('ui.saveTheme', 'Save Theme')}</button>
                </div>
            </form>
        </div>
    `;

    document.body.appendChild(modal);
    document.getElementById('close-theme-modal').addEventListener('click', closeThemeModal);
    document.getElementById('cancel-theme').addEventListener('click', closeThemeModal);
    document.getElementById('theme-form').addEventListener('submit', handleThemeSubmit);
    modal.addEventListener('click', (e) => { if (e.target.id === 'theme-modal') closeThemeModal(); });

    colorFields.forEach(f => {
        document.getElementById(f.key).addEventListener('input', updatePreview);
    });
}

function openThemeModal() {
    createThemeModal();
    document.getElementById('theme-modal').classList.remove('hidden');
}

function closeThemeModal() {
    const modal = document.getElementById('theme-modal');
    if (modal) modal.classList.add('hidden');
    const form = document.getElementById('theme-form');
    if (form) form.reset();
}

function updatePreview() {
    const preview = document.getElementById('theme-live-preview');
    if (!preview) return;
    const obj = {};
    colorFields.forEach(f => obj[f.key] = document.getElementById(f.key).value);
    preview.style.setProperty('--primary', obj.primaryColor);
    preview.style.setProperty('--secondary', obj.secondaryColor);
    preview.style.setProperty('--accent', obj.accentColor);
    preview.style.setProperty('--background', obj.backgroundColor);
    preview.style.setProperty('--text', obj.textColor);
}

function renderThemes() {
    if (!uiSection) return;
    const container = uiSection.querySelector('.themes-list');
    if (!themes.length) {
        container.innerHTML = `<div class="empty-state">${t('ui.noThemes', 'No themes available.')}</div>`;
        return;
    }

    container.innerHTML = themes.map(theme => `
        <article class="theme-card" data-id="${theme.id}">
            <div class="theme-swatch">
                <div class="swatch-col" style="background:${theme.primaryColor}"></div>
                <div class="swatch-col" style="background:${theme.secondaryColor}"></div>
                <div class="swatch-col" style="background:${theme.accentColor}"></div>
                <div class="swatch-col" style="background:${theme.backgroundColor}"></div>
                <div class="swatch-col" style="background:${theme.textColor}"></div>
            </div>
            <div class="theme-meta">
                <h4>${theme.name}</h4>
                <div class="small-muted">${theme.description || ''}</div>
            </div>
            <div class="theme-actions">
                ${theme.isActive ? `<span class="badge">${t('ui.active', 'Active')}</span>` : ''}
                ${isProtectedTheme(theme) ? `<span class="badge">${t('ui.defaultTheme', 'Default Theme')}</span>` : ''}
                <button class="mini-button" data-action="use" data-id="${theme.id}">${t('ui.useTheme', 'Use This Theme')}</button>
                ${!isProtectedTheme(theme) ? `<button class="mini-button secondary" data-action="edit" data-id="${theme.id}">${t('ui.editTheme', 'Edit')}</button>` : ''}
                ${!isProtectedTheme(theme) ? `<button class="mini-button danger" data-action="delete" data-id="${theme.id}">${t('ui.deleteTheme', 'Delete')}</button>` : ''}
            </div>
        </article>
    `).join('');

    container.querySelectorAll('[data-action="use"]').forEach(btn => btn.addEventListener('click', () => activateTheme(btn.dataset.id)));
    container.querySelectorAll('[data-action="edit"]').forEach(btn => btn.addEventListener('click', () => editTheme(btn.dataset.id)));
    container.querySelectorAll('[data-action="delete"]').forEach(btn => btn.addEventListener('click', () => deleteTheme(btn.dataset.id)));
}

async function loadThemes() {
    if (!uiSection) return;
    const container = uiSection.querySelector('.themes-list');
    container.innerHTML = `<div class="empty-state">${t('ui.loading', 'Loading themes…')}</div>`;

    try {
        const resp = await fetch('/api/themes', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } });
        if (!resp.ok) throw new Error('Unable to load themes');
        themes = await resp.json();
        // find active
        const activeResp = await fetch('/api/themes/active', {
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
        });
        if (activeResp.ok) {
            const active = await activeResp.json();
            activeThemeId = active.id;
        }
        renderThemes();
    } catch (err) {
        container.innerHTML = `<div class="empty-state">${t('ui.unable', 'Unable to load themes right now.')}</div>`;
    }
}

async function activateTheme(id) {
    const resp = await fetch(`/api/themes/${id}/activate`, { method: 'PUT', headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } });
    if (resp.ok) {
        // apply immediately
        const theme = themes.find(t => t.id === id);
        if (theme) {
            if (window.applyTheme) window.applyTheme(theme);
            activeThemeId = id;
            renderThemes();
        }
    }
}

function editTheme(id) {
    const theme = themes.find(t => t.id === id);
    if (!theme || isProtectedTheme(theme)) return;
    openThemeModal();
    document.getElementById('theme-modal-title').textContent = t('ui.editTheme', 'Edit Theme');
    document.getElementById('theme-name').value = theme.name || '';
    colorFields.forEach(f => document.getElementById(f.key).value = theme[f.key] || '#ffffff');
    updatePreview();
    document.getElementById('theme-form').dataset.editing = id;
}

async function handleThemeSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('theme-form').dataset.editing;
    const payload = {
        Name: document.getElementById('theme-name').value.trim(),
        PrimaryColor: document.getElementById('primaryColor').value,
        SecondaryColor: document.getElementById('secondaryColor').value,
        AccentColor: document.getElementById('accentColor').value,
        BackgroundColor: document.getElementById('backgroundColor').value,
        TextColor: document.getElementById('textColor').value
    };

    const resp = await fetch(id ? `/api/themes/${id}` : '/api/themes', {
        method: id ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` },
        body: JSON.stringify(payload)
    });

    if (resp.ok) {
        closeThemeModal();
        delete document.getElementById('theme-form').dataset.editing;
        loadThemes();
    }
}

async function deleteTheme(id) {
    const theme = themes.find(t => t.id === id);
    if (!theme || isProtectedTheme(theme)) return;
    if (!window.confirm(t('ui.deleteConfirm', 'Delete this theme?'))) return;
    const resp = await fetch(`/api/themes/${id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } });
    if (resp.ok) loadThemes();
}

function bindUISection() {
    if (!uiSection) return;
    const addBtn = uiSection.querySelector('.add-theme-button');
    if (addBtn) addBtn.addEventListener('click', () => {
        document.getElementById('theme-modal')?.remove();
        createThemeModal();
        document.getElementById('theme-modal').classList.remove('hidden');
        document.getElementById('theme-form').removeAttribute('data-editing');
        document.getElementById('theme-modal-title').textContent = t('ui.addTheme', 'Add Theme');
        colorFields.forEach(f => document.getElementById(f.key).value = '#ffffff');
        updatePreview();
    });
}

window.addEventListener('DOMContentLoaded', () => {
    bindUISection();
    loadThemes();
});

window.addEventListener('qore-language-changed', () => {
    renderThemes();
});
})();
