(function() {
const adminSection = document.querySelector('[data-section-content="admin"]');

let adminUsers = [];
let backupStatusState = null;
const MIN_BACKUP_THRESHOLD_GB = 50;
const MAX_BACKUP_THRESHOLD_GB = 150;

const t = (key, fallback) => window.t ? window.t(key, fallback) : fallback;

function normalizeRole(role) {
    return String(role || '').trim().toLowerCase();
}

function isAdminRole(role) {
    return normalizeRole(role) === 'admin';
}

function applyAdminVisibility(role = window.currentUser?.role || 'Employee') {
    const isAdmin = isAdminRole(role);

    document.querySelectorAll('.admin-only').forEach((item) => {
        item.hidden = !isAdmin;
        item.style.display = isAdmin ? '' : 'none';
    });

    const adminNavButton = document.querySelector('.nav-item[data-section="admin"]');
    if (adminNavButton) {
        adminNavButton.hidden = !isAdmin;
        adminNavButton.style.display = isAdmin ? '' : 'none';
    }

    const adminSection = document.querySelector('[data-section-content="admin"]');
    if (adminSection) {
        adminSection.hidden = !isAdmin;
        adminSection.setAttribute('aria-hidden', String(!isAdmin));
        adminSection.style.display = isAdmin ? '' : 'none';
    }
}

window.applyAdminVisibility = applyAdminVisibility;

function showToast(message, isError = false) {
    let toast = document.getElementById('admin-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'admin-toast';
        toast.className = 'toast';
        document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.classList.toggle('error', !!isError);
    toast.classList.add('show');
    window.clearTimeout(showToast.timeout);
    showToast.timeout = window.setTimeout(() => toast.classList.remove('show'), 2000);
}

function requireAdmin() {
    const role = window.currentUser?.role || 'Employee';
    const isDashboardPage = window.location.pathname === '/dashboard.html' || window.location.pathname === '/';

    applyAdminVisibility(role);

    if (!isAdminRole(role)) {
        if (!isDashboardPage) {
            window.location.href = '/dashboard.html';
        }
        return false;
    }

    return true;
}

function createUserModal() {
    if (document.getElementById('user-modal')) return;

    const modal = document.createElement('div');
    modal.id = 'user-modal';
    modal.className = 'modal hidden';
    modal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header">
                <h3 id="user-modal-title">${t('admin.modalTitleCreate', 'Create User')}</h3>
                <button class="icon-button" id="close-user-modal">×</button>
            </div>
            <form id="user-form" class="modal-form">
                <input type="hidden" id="user-id" />
                <label class="modal-field"><span>${t('admin.fullName', 'Full Name')}</span><input id="user-fullname" required /></label>
                <label class="modal-field"><span>${t('admin.username', 'Username')}</span><input id="user-username" required /></label>
                <label class="modal-field"><span>${t('admin.email', 'Email')}</span><input id="user-email" type="email" required /></label>
                <label class="modal-field"><span>${t('admin.password', 'Password')}</span><input id="user-password" type="password" /></label>
                <label class="modal-field"><span>${t('admin.role', 'Role')}</span>
                    <select id="user-role-select">
                        <option value="Admin">${t('admin.roleAdmin', 'Admin')}</option>
                        <option value="Manager">${t('admin.roleManager', 'Manager')}</option>
                        <option value="Employee">${t('admin.roleEmployee', 'Employee')}</option>
                    </select>
                </label>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-user" type="button">${t('common.cancel', 'Cancel')}</button>
                    <button class="primary-button" type="submit">${t('common.save', 'Save')}</button>
                </div>
            </form>
        </div>
    `;

    document.body.appendChild(modal);
    document.getElementById('close-user-modal').addEventListener('click', closeUserModal);
    document.getElementById('cancel-user').addEventListener('click', closeUserModal);
    document.getElementById('user-form').addEventListener('submit', handleUserSubmit);
    modal.addEventListener('click', (e) => { if (e.target.id === 'user-modal') closeUserModal(); });
}

function openCreateUserModal() {
    createUserModal();
    document.getElementById('user-modal').classList.remove('hidden');
    document.getElementById('user-modal-title').textContent = t('admin.modalTitleCreate', 'Create User');
    document.getElementById('user-id').value = '';
    document.getElementById('user-password').required = true;
}

function openEditUserModal(userId) {
    createUserModal();
    const user = adminUsers.find(u => u.id === userId);
    if (!user) return;
    document.getElementById('user-modal').classList.remove('hidden');
    document.getElementById('user-modal-title').textContent = t('admin.modalTitleEdit', 'Edit User');
    document.getElementById('user-id').value = user.id;
    document.getElementById('user-fullname').value = user.fullName || '';
    document.getElementById('user-username').value = user.username || '';
    document.getElementById('user-email').value = user.email || '';
    document.getElementById('user-role-select').value = user.role || 'Employee';
    document.getElementById('user-password').required = false;
}

function closeUserModal() {
    const modal = document.getElementById('user-modal');
    if (modal) modal.classList.add('hidden');
    const form = document.getElementById('user-form');
    if (form) form.reset();
}

async function handleUserSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('user-id').value;
    const selectedRole = document.getElementById('user-role-select').value || 'Employee';
    const payload = {
        username: document.getElementById('user-username').value.trim(),
        fullName: document.getElementById('user-fullname').value.trim(),
        email: document.getElementById('user-email').value.trim(),
        role: selectedRole,
        password: document.getElementById('user-password').value
    };

    const url = id ? `/api/admin/users/${id}` : '/api/admin/users';
    const method = id ? 'PUT' : 'POST';

    try {
        const resp = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` },
            body: JSON.stringify(payload)
        });
        if (resp.ok) {
            showToast(id ? t('admin.userUpdated', 'User updated') : t('admin.userCreated', 'User created'));
            closeUserModal();
            loadUsers();
        } else {
            const err = await resp.json().catch(() => null);
            showToast(err?.message || t('admin.saveUserFailed', 'Unable to save user'), true);
        }
    } catch (err) {
        showToast('Unable to reach server', true);
    }
}

function createResetModal() {
    if (document.getElementById('reset-modal')) return;
    const modal = document.createElement('div');
    modal.id = 'reset-modal';
    modal.className = 'modal hidden';
    modal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header"><h3>${t('admin.resetPassword', 'Reset Password')}</h3><button class="icon-button" id="close-reset-modal">×</button></div>
            <form id="reset-form" class="modal-form">
                <input type="hidden" id="reset-user-id" />
                <label class="modal-field"><span>${t('admin.newPassword', 'New Password')}</span><input id="reset-new-password" type="password" required minlength="8" /></label>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-reset" type="button">${t('common.cancel', 'Cancel')}</button>
                    <button class="primary-button" type="submit">${t('common.reset', 'Reset')}</button>
                </div>
            </form>
        </div>
    `;
    document.body.appendChild(modal);
    document.getElementById('close-reset-modal').addEventListener('click', () => document.getElementById('reset-modal').classList.add('hidden'));
    document.getElementById('cancel-reset').addEventListener('click', () => document.getElementById('reset-modal').classList.add('hidden'));
    document.getElementById('reset-form').addEventListener('submit', handleResetSubmit);
}

function openResetModal(userId) {
    createResetModal();
    document.getElementById('reset-user-id').value = userId;
    document.getElementById('reset-modal').classList.remove('hidden');
}

async function handleResetSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('reset-user-id').value;
    const newPassword = document.getElementById('reset-new-password').value;
    try {
        const resp = await fetch(`/api/admin/users/${id}/reset-password`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` },
            body: JSON.stringify({ newPassword })
        });
        if (resp.ok) {
            showToast(t('admin.passwordReset', 'Password reset'));
            document.getElementById('reset-modal').classList.add('hidden');
        } else {
            const err = await resp.json().catch(() => null);
            showToast(err?.message || t('admin.resetFailed', 'Reset failed'), true);
        }
    } catch (err) {
        showToast(t('common.serverError', 'Unable to reach server'), true);
    }
}

async function toggleActive(userId, makeActive) {
    if (!makeActive && !window.confirm(t('admin.confirmDeactivate', 'Are you sure you want to deactivate this user?'))) return;

    const url = `/api/admin/users/${userId}/${makeActive ? 'activate' : 'deactivate'}`;
    try {
        const resp = await fetch(url, { method: 'PUT', headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } });
        if (resp.ok) {
            showToast(makeActive ? t('admin.userActivated', 'User activated') : t('admin.userDeactivated', 'User deactivated'));
            loadUsers();
        } else {
            const err = await resp.json().catch(() => null);
            showToast(err?.message || t('admin.operationFailed', 'Operation failed'), true);
        }
    } catch (err) {
        showToast(t('common.serverError', 'Unable to reach server'), true);
    }
}

async function deleteUser(userId) {
    if (!window.confirm(t('admin.deleteConfirm', 'Delete this user? This action cannot be undone.'))) return;
    try {
        const resp = await fetch(`/api/admin/users/${userId}`, { method: 'DELETE', headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } });
        if (resp.ok) {
            showToast(t('admin.userDeleted', 'User deleted'));
            loadUsers();
        } else {
            const err = await resp.json().catch(() => null);
            showToast(err?.message || t('admin.deleteFailed', 'Delete failed'), true);
        }
    } catch (err) {
        showToast(t('common.serverError', 'Unable to reach server'), true);
    }
}

function renderUsers() {
    if (!adminSection) return;
    const container = adminSection.querySelector('.admin-users-list');
    if (!adminUsers.length) {
        container.innerHTML = `<div class="empty-state">${t('admin.noUsers', 'No users found.')}</div>`;
        return;
    }

    container.innerHTML = `
        <div class="staff-table-wrapper">
            <table class="staff-table admin-table">
                <thead>
                    <tr>
                        <th>${t('admin.fullName', 'Full Name')}</th>
                        <th>${t('admin.username', 'Username')}</th>
                        <th>${t('admin.email', 'Email')}</th>
                        <th>${t('admin.role', 'Role')}</th>
                        <th>${t('admin.status', 'Status')}</th>
                        <th>${t('admin.actions', 'Actions')}</th>
                    </tr>
                </thead>
                <tbody>
                    ${adminUsers.map(u => `
                        <tr class="staff-row">
                            <td>${u.fullName}</td>
                            <td>${u.username}</td>
                            <td>${u.email}</td>
                            <td>${u.role}</td>
                            <td>${u.isActive ? `<span class="badge">${t('admin.active', 'Active')}</span>` : `<span class="badge">${t('admin.inactive', 'Inactive')}</span>`}</td>
                            <td class="staff-actions">
                                <button class="mini-button" data-action="edit" data-id="${u.id}">${t('admin.edit', 'Edit')}</button>
                                <button class="mini-button" data-action="reset" data-id="${u.id}">${t('admin.resetPasswordAction', 'Reset Password')}</button>
                                ${u.isActive ? `<button class="mini-button secondary" data-action="deactivate" data-id="${u.id}">${t('admin.deactivate', 'Deactivate')}</button>` : `<button class="mini-button" data-action="activate" data-id="${u.id}">${t('admin.activate', 'Activate')}</button>`}
                                <button class="mini-button danger" data-action="delete" data-id="${u.id}">${t('admin.delete', 'Delete')}</button>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>
    `;

    container.querySelectorAll('[data-action="edit"]').forEach(btn => btn.addEventListener('click', () => openEditUserModal(btn.dataset.id)));
    container.querySelectorAll('[data-action="reset"]').forEach(btn => btn.addEventListener('click', () => openResetModal(btn.dataset.id)));
    container.querySelectorAll('[data-action="deactivate"]').forEach(btn => btn.addEventListener('click', () => toggleActive(btn.dataset.id, false)));
    container.querySelectorAll('[data-action="activate"]').forEach(btn => btn.addEventListener('click', () => toggleActive(btn.dataset.id, true)));
    container.querySelectorAll('[data-action="delete"]').forEach(btn => btn.addEventListener('click', () => deleteUser(btn.dataset.id)));
}

async function loadUsers() {
    if (!adminSection) return;
    if (!requireAdmin()) return;
    const container = adminSection.querySelector('.admin-users-list');
    container.innerHTML = `<div class="empty-state">${t('admin.loadingUsers', 'Loading users…')}</div>`;
    try {
        const resp = await fetch('/api/admin/users', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } });
        if (!resp.ok) throw new Error('Unable to load users');
        adminUsers = await resp.json();
        renderUsers();
    } catch (err) {
        container.innerHTML = `<div class="empty-state">${t('admin.loadUsersFailed', 'Unable to load users right now.')}</div>`;
    }
}

function bindAdminSection() {
    if (!adminSection) return;
    if (!requireAdmin()) return;
    const createBtn = adminSection.querySelector('.create-user-button');
    if (createBtn) createBtn.addEventListener('click', openCreateUserModal);

    const backupStatusButton = adminSection.querySelector('.backup-status-button');
    if (backupStatusButton) backupStatusButton.addEventListener('click', loadBackupStatus);

    const backupNowButton = adminSection.querySelector('.backup-now-button');
    if (backupNowButton) backupNowButton.addEventListener('click', createBackup);

    const thresholdForm = adminSection.querySelector('#backup-threshold-form');
    if (thresholdForm) thresholdForm.addEventListener('submit', handleThresholdSubmit);

    const restoreForm = adminSection.querySelector('#restore-form');
    if (restoreForm) restoreForm.addEventListener('submit', handleRestoreSubmit);

    const connectDatabaseButton = adminSection.querySelector('#connect-database-button');
    if (connectDatabaseButton) connectDatabaseButton.addEventListener('click', handleConnectDatabase);

    const restoreResetButton = adminSection.querySelector('#restore-reset');
    if (restoreResetButton) restoreResetButton.addEventListener('click', () => restoreForm.reset());
}

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function coerceThresholdValue(value) {
    const parsed = Number.parseInt(String(value ?? ''), 10);
    if (Number.isNaN(parsed)) {
        return null;
    }

    return Math.min(MAX_BACKUP_THRESHOLD_GB, Math.max(MIN_BACKUP_THRESHOLD_GB, parsed));
}

function setThresholdSelectValue(selectEl, thresholdValue) {
    if (!selectEl || thresholdValue === null) {
        return;
    }

    const exact = Array.from(selectEl.options).some((option) => option.value === String(thresholdValue));
    if (exact) {
        selectEl.value = String(thresholdValue);
        return;
    }

    let nearest = String(MIN_BACKUP_THRESHOLD_GB);
    let nearestDiff = Number.POSITIVE_INFINITY;
    Array.from(selectEl.options).forEach((option) => {
        const optionValue = Number.parseInt(option.value, 10);
        const diff = Math.abs(optionValue - thresholdValue);
        if (diff < nearestDiff) {
            nearestDiff = diff;
            nearest = option.value;
        }
    });
    selectEl.value = nearest;
}

async function downloadBackupFile(downloadUrl, fileName) {
    const token = sessionStorage.getItem('qore_token') || '';
    if (!downloadUrl || !token) {
        throw new Error(t('admin.downloadFailed', 'Unable to download backup file.'));
    }

    const response = await fetch(downloadUrl, {
        method: 'GET',
        headers: {
            Authorization: `Bearer ${token}`
        }
    });

    if (!response.ok) {
        throw new Error(t('admin.downloadFailed', 'Unable to download backup file.'));
    }

    const blob = await response.blob();
    const objectUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = fileName || 'backup.bak';
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(objectUrl);
}

function renderBackupStatus() {
    if (!adminSection) return;
    const statusEl = adminSection.querySelector('#backup-status');
    if (!statusEl) return;

    if (!backupStatusState) {
        statusEl.innerHTML = `
            <div class="backup-status-card backup-status-info">
                <span class="backup-status-label">${t('admin.backupStatusReadyLabel', 'Backup Status')}</span>
                <p>${t('admin.backupStatusReady', 'Ready to create or restore a backup.')}</p>
            </div>
        `;
        return;
    }

    const stateClass = backupStatusState.kind === 'error' ? 'backup-status-error' : (backupStatusState.kind === 'success' ? 'backup-status-success' : 'backup-status-info');
    const title = backupStatusState.titleKey
        ? t(backupStatusState.titleKey, backupStatusState.titleFallback || 'Backup Status')
        : (backupStatusState.title || t('admin.backupStatusReadyLabel', 'Backup Status'));
    const message = backupStatusState.messageKey
        ? t(backupStatusState.messageKey, backupStatusState.messageFallback || '')
        : (backupStatusState.message || '');
    const details = Array.isArray(backupStatusState.details) ? backupStatusState.details : [];
    const download = backupStatusState.download || null;

    const detailsHtml = details.length
        ? `<div class="backup-status-grid">${details
            .map((item) => {
                const label = item.labelKey ? t(item.labelKey, item.labelFallback || '') : item.label;
                return `<div class="backup-meta-pill"><span>${escapeHtml(label)}</span><strong>${escapeHtml(item.value)}</strong></div>`;
            })
            .join('')}</div>`
        : '';

    const downloadHtml = download
        ? `<button type="button" class="mini-button backup-download-button" data-download-url="${escapeHtml(download.url)}" data-file-name="${escapeHtml(download.fileName || 'backup.bak')}">${t('admin.downloadBackup', 'Download Backup')}</button>`
        : '';

    statusEl.innerHTML = `
        <div class="backup-status-card ${stateClass}">
            <span class="backup-status-label">${escapeHtml(title)}</span>
            <p>${escapeHtml(message)}</p>
            ${detailsHtml}
            ${downloadHtml}
        </div>
    `;

    const downloadButton = statusEl.querySelector('.backup-download-button');
    if (downloadButton) {
        downloadButton.addEventListener('click', async () => {
            try {
                downloadButton.disabled = true;
                await downloadBackupFile(downloadButton.dataset.downloadUrl, downloadButton.dataset.fileName);
                showToast(t('admin.downloadStarted', 'Backup download started.'));
            } catch (error) {
                showToast(error?.message || t('admin.downloadFailed', 'Unable to download backup file.'), true);
            } finally {
                downloadButton.disabled = false;
            }
        });
    }
}

async function loadBackupStatus() {
    if (!adminSection) return;
    const statusEl = adminSection.querySelector('#backup-status');
    if (!statusEl) return;

    try {
        const resp = await fetch('/api/system/backup-status', {
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
        });
        if (!resp.ok) {
            const errText = await resp.text().catch(() => '');
            throw new Error(errText || 'Unable to load backup status');
        }
        const data = await resp.json();
        const sizeText = data?.currentSizeGb != null ? `${Number(data.currentSizeGb).toFixed(2)} GB` : t('common.unavailable', 'Unavailable');
        const thresholdInput = adminSection.querySelector('#backup-threshold-gb');
        const thresholdValue = coerceThresholdValue(data?.thresholdGb);
        setThresholdSelectValue(thresholdInput, thresholdValue);
        backupStatusState = {
            kind: 'info',
            titleKey: 'admin.backupStatusLoadedTitle',
            titleFallback: 'Backup status loaded',
            messageKey: data?.backupRecommended ? 'admin.backupRecommended' : 'admin.backupCurrent',
            messageFallback: data?.backupRecommended ? 'A backup is recommended.' : 'Current size is within the recommended threshold.',
            details: [
                { labelKey: 'admin.databaseLabel', labelFallback: 'Database', value: data?.currentDatabase || 'QoreDB' },
                { labelKey: 'admin.databaseSizeLabel', labelFallback: 'Size', value: sizeText },
                { labelKey: 'admin.thresholdLabel', labelFallback: 'Threshold', value: `${Number(data?.thresholdGb || 0).toFixed(2)} GB` }
            ]
        };
        renderBackupStatus();
    } catch (err) {
        backupStatusState = {
            kind: 'error',
            titleKey: 'admin.backupFailedTitle',
            titleFallback: 'Backup status error',
            message: err?.message || t('admin.backupFailed', 'Unable to create a backup.')
        };
        renderBackupStatus();
    }
}

async function handleConnectDatabase() {
    if (!adminSection) return;

    const databaseInput = adminSection.querySelector('#connect-database-name');
    if (!databaseInput) return;

    const databaseName = String(databaseInput.value || '').trim();
    if (!databaseName) {
        showToast(t('admin.connectDatabaseNameRequired', 'Enter a target database name first.'), true);
        databaseInput.focus();
        return;
    }

    try {
        const resp = await fetch('/api/system/connect-database', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
            },
            body: JSON.stringify({ databaseName })
        });

        const contentType = resp.headers.get('content-type') || '';
        const data = contentType.includes('application/json') ? await resp.json().catch(() => null) : null;
        if (!resp.ok) {
            throw new Error((data && data.message) || t('admin.connectDatabaseFailed', 'Failed to connect to the selected database.'));
        }

        backupStatusState = {
            kind: 'success',
            titleKey: 'admin.connectDatabaseStartedTitle',
            titleFallback: 'Database connection update started',
            message: data?.message || t('admin.connectDatabaseStarted', 'Database connection update started. The application will reconnect after restart.'),
            details: [
                { labelKey: 'admin.databaseLabel', labelFallback: 'Database', value: databaseName }
            ]
        };
        renderBackupStatus();
    } catch (err) {
        showToast(err?.message || t('admin.connectDatabaseFailed', 'Failed to connect to the selected database.'), true);
    }
}

async function handleThresholdSubmit(e) {
    e.preventDefault();
    if (!adminSection) return;

    const thresholdInput = adminSection.querySelector('#backup-threshold-gb');
    if (!thresholdInput) return;

    const parsed = Number.parseInt(String(thresholdInput.value ?? '').trim(), 10);
    if (Number.isNaN(parsed) || parsed < MIN_BACKUP_THRESHOLD_GB || parsed > MAX_BACKUP_THRESHOLD_GB) {
        showToast(
            t(
                'admin.thresholdValidation',
                `Threshold must be between ${MIN_BACKUP_THRESHOLD_GB} and ${MAX_BACKUP_THRESHOLD_GB} GB.`
            ),
            true
        );
        thresholdInput.focus();
        return;
    }

    try {
        const resp = await fetch('/api/system/backup-threshold', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
            },
            body: JSON.stringify({ thresholdGb: parsed })
        });

        const contentType = resp.headers.get('content-type') || '';
        const data = contentType.includes('application/json') ? await resp.json().catch(() => null) : null;
        if (!resp.ok) {
            throw new Error((data && data.message) || t('admin.thresholdSaveFailed', 'Unable to save threshold.'));
        }

        showToast(data?.message || t('admin.thresholdSaved', 'Threshold saved.'));
        await loadBackupStatus();
    } catch (err) {
        showToast(err?.message || t('admin.thresholdSaveFailed', 'Unable to save threshold.'), true);
    }
}

async function createBackup() {
    if (!adminSection) return;
    const statusEl = adminSection.querySelector('#backup-status');
    if (!statusEl) return;

    try {
        const resp = await fetch('/api/system/backup', {
            method: 'POST',
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
        });
        const contentType = resp.headers.get('content-type') || '';
        const data = contentType.includes('application/json') ? await resp.json().catch(() => null) : null;
        if (!resp.ok) throw new Error((data && data.message) || t('admin.backupFailed', 'Unable to create a backup.'));

        const downloadUrl = data?.downloadUrl || '';
        const backupFileName = data?.fileName || 'backup.bak';
        backupStatusState = {
            kind: 'success',
            titleKey: 'admin.backupCreatedTitle',
            titleFallback: 'Backup created successfully',
            messageKey: 'admin.backupCreated',
            messageFallback: 'Backup created successfully.',
            details: [
                { labelKey: 'admin.backupFileLabel', labelFallback: 'File', value: backupFileName },
                { labelKey: 'admin.databaseSizeLabel', labelFallback: 'Size', value: `${Number(data?.sizeGb || 0).toFixed(2)} GB` }
            ],
            download: downloadUrl ? { url: downloadUrl, fileName: backupFileName } : null
        };
        renderBackupStatus();

        if (downloadUrl) {
            try {
                await downloadBackupFile(downloadUrl, backupFileName);
                showToast(t('admin.downloadStarted', 'Backup download started.'));
            } catch (downloadErr) {
                showToast(downloadErr?.message || t('admin.downloadFailed', 'Unable to download backup file.'), true);
            }
        }
    } catch (err) {
        backupStatusState = {
            kind: 'error',
            titleKey: 'admin.backupFailedTitle',
            titleFallback: 'Backup failed',
            message: err?.message || t('admin.backupFailed', 'Unable to create a backup.')
        };
        renderBackupStatus();
    }
}

async function handleRestoreSubmit(e) {
    e.preventDefault();
    if (!adminSection) return;
    const statusEl = adminSection.querySelector('#backup-status');
    const fileInput = adminSection.querySelector('#restore-file');
    const databaseInput = adminSection.querySelector('#restore-database-name');
    if (!statusEl || !fileInput || !databaseInput) return;

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);
    formData.append('databaseName', databaseInput.value.trim());

    try {
        const resp = await fetch('/api/system/restore', {
            method: 'POST',
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` },
            body: formData
        });
        const contentType = resp.headers.get('content-type') || '';
        const data = contentType.includes('application/json') ? await resp.json().catch(() => null) : null;
        if (!resp.ok) throw new Error((data && data.message) || t('admin.restoreFailed', 'Restore failed. Please verify the backup file and SQL Server permissions.'));
        backupStatusState = {
            kind: 'success',
            titleKey: 'admin.restoreStartedTitle',
            titleFallback: 'Restore started',
            message: data?.message || t('admin.restoreStarted', 'Restore started. The application will reconnect after restart.'),
            details: [
                { labelKey: 'admin.databaseLabel', labelFallback: 'Database', value: databaseInput.value.trim() || t('common.unavailable', 'Unavailable') }
            ]
        };
        renderBackupStatus();
        if (databaseInput.value) {
            databaseInput.value = '';
        }
        fileInput.value = '';
    } catch (err) {
        backupStatusState = {
            kind: 'error',
            titleKey: 'admin.restoreFailedTitle',
            titleFallback: 'Restore failed',
            message: err?.message || t('admin.restoreFailed', 'Restore failed. Please verify the backup file and SQL Server permissions.')
        };
        renderBackupStatus();
    }
}

function initAdminSection() {
    bindAdminSection();
    loadUsers();
    renderBackupStatus();
    loadBackupStatus();
}

window.addEventListener('DOMContentLoaded', () => {
    initAdminSection();
});

window.addEventListener('qore-auth-ready', () => {
    initAdminSection();
});

window.addEventListener('qore-language-changed', () => {
    renderUsers();
    renderBackupStatus();
});
})();
