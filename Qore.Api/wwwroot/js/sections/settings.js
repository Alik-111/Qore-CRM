const settingsSection = document.querySelector('[data-section-content="settings"]');

function buildLanguageSelector() {
    const currentLanguage = window.qoreI18n?.getLanguage?.() || 'en';
    const options = Object.entries(window.qoreI18n?.languageNames || {}).map(([code, name]) => `<option value="${code}" ${code === currentLanguage ? 'selected' : ''}>${name}</option>`).join('');
    return `
        <div class="settings-card">
            <h4 data-i18n="common.language">Language</h4>
            <div class="detail-grid">
                <div>
                    <strong data-i18n="common.selectLanguage">Select language</strong>
                    <select id="language-selector" class="language-selector">
                        ${options}
                    </select>
                </div>
            </div>
        </div>
    `;
}

function formatUserInfo() {
    const user = window.currentUser?.storedUser || window.currentUser?.raw || {};
    return {
        username: user.username || user.userName || window.currentUser?.username || 'User',
        fullName: user.fullName || user.name || '—',
        email: user.email || '—',
        role: window.currentUser?.role || 'User'
    };
}

function showToast(message, isError = false) {
    let toast = document.getElementById('settings-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'settings-toast';
        toast.className = 'toast';
        document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.classList.toggle('error', !!isError);
    toast.classList.add('show');
    window.clearTimeout(showToast.timeout);
    showToast.timeout = window.setTimeout(() => toast.classList.remove('show'), 2200);
}

function renderSettings() {
    if (!settingsSection) return;
    const info = formatUserInfo();
    settingsSection.querySelector('.settings-list').innerHTML = `
        ${buildLanguageSelector()}
        <div class="settings-card">
            <h4 data-i18n="common.account">Account</h4>
            <div class="detail-grid">
                <div><strong data-i18n="auth.username">Username</strong><span>${info.username}</span></div>
                <div><strong data-i18n="common.fullName">Full Name</strong><span>${info.fullName}</span></div>
                <div><strong data-i18n="common.email">Email</strong><span>${info.email}</span></div>
                <div><strong data-i18n="common.role">Role</strong><span>${info.role}</span></div>
            </div>
            <h4 style="margin-top:16px" data-i18n="common.changePassword">Change Password</h4>
            <form id="change-password-form" class="modal-form">
                <label class="modal-field"><span data-i18n="common.currentPassword">Current Password</span><input id="current-password" type="password" required /></label>
                <label class="modal-field"><span data-i18n="common.newPassword">New Password</span><input id="new-password" type="password" required minlength="8" /></label>
                <label class="modal-field"><span data-i18n="common.confirmPassword">Confirm New Password</span><input id="confirm-password" type="password" required minlength="8" /></label>
                <div class="modal-actions">
                    <button class="primary-button" type="submit" data-i18n="common.changePassword">Change Password</button>
                </div>
                <div id="change-password-message" class="small-muted" style="margin-top:8px"></div>
            </form>
        </div>
    `;

    const form = document.getElementById('change-password-form');
    form?.addEventListener('submit', handleChangePassword);

    const selector = document.getElementById('language-selector');
    selector?.addEventListener('change', (event) => {
        window.qoreI18n?.setLanguage?.(event.target.value);
        renderSettings();
    });

    window.qoreI18n?.applyTranslations?.(settingsSection);
}

window.addEventListener('qore-language-changed', () => {
    renderSettings();
});

async function handleChangePassword(event) {
    event.preventDefault();
    const current = document.getElementById('current-password').value;
    const nw = document.getElementById('new-password').value;
    const confirm = document.getElementById('confirm-password').value;
    const messageEl = document.getElementById('change-password-message');
    messageEl.textContent = '';

    if (nw !== confirm) {
        messageEl.textContent = window.t ? window.t('common.passwordMismatch', 'New password and confirmation do not match.') : 'New password and confirmation do not match.';
        return;
    }
    if (nw.length < 8) {
        messageEl.textContent = window.t ? window.t('common.passwordTooShort', 'New password must be at least 8 characters.') : 'New password must be at least 8 characters.';
        return;
    }

    try {
        const resp = await fetch('/api/auth/change-password', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
            },
            body: JSON.stringify({ currentPassword: current, newPassword: nw })
        });

        if (resp.ok) {
            showToast(window.t ? window.t('common.passwordChanged', 'Password changed') : 'Password changed');
            document.getElementById('change-password-form').reset();
        } else {
            const err = await resp.json().catch(() => null);
            const msg = err?.message || err?.error || (window.t ? window.t('common.serverError', 'Unable to reach server') : 'Unable to reach server');
            showToast(msg, true);
            messageEl.textContent = msg;
        }
    } catch (e) {
        showToast(window.t ? window.t('common.serverError', 'Unable to reach server') : 'Unable to reach server', true);
    }
}

function bindSettings() {
    renderSettings();
}

window.addEventListener('DOMContentLoaded', () => bindSettings());
