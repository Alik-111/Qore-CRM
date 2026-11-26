(function() {
    const documentsSection = document.querySelector('[data-section-content="documents"]');

    let documentsList = [];
    let users = [];
    let documentsSearchQuery = '';

    const t = (key, fallback) => (window.t ? window.t(key, fallback) : fallback);
    const visibilityOptions = [
        { value: 'Everyone', key: 'documents.visibilityEveryone', fallback: 'Everyone' },
        { value: 'Admin', key: 'documents.visibilityAdmin', fallback: 'Admin' },
        { value: 'Manager', key: 'documents.visibilityManager', fallback: 'Manager' },
        { value: 'Employee', key: 'documents.visibilityEmployee', fallback: 'Employee' }
    ];

    function formatDate(value) {
    if (!value) return '—';
    return new Date(value).toLocaleString();
}

function getUserName(id) {
    const user = users.find(u => u.id === id);
    return user ? user.fullName : t('documents.unknownUser', 'Unknown');
}

function canDeleteDocument(doc) {
    const role = window.currentUser?.role || 'Employee';
    if (role === 'Admin' || role === 'Manager') return true;
    const currentId = window.currentUser?.storedUser?.id || window.currentUser?.raw?.nameid;
    return doc.uploadedByUserId === currentId;
}

function showToast(message) {
    let toast = document.getElementById('doc-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'doc-toast';
        toast.className = 'toast';
        document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.classList.add('show');
    window.clearTimeout(showToast.timeout);
    showToast.timeout = window.setTimeout(() => toast.classList.remove('show'), 2000);
}

function createUploadModal() {
    if (document.getElementById('document-modal')) return;

    const modal = document.createElement('div');
    modal.id = 'document-modal';
    modal.className = 'modal hidden';
    modal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header">
                <h3 id="document-modal-title">${window.t ? window.t('documents.modalTitle', 'Add Document') : 'Add Document'}</h3>
                <button class="icon-button" id="close-document-modal">×</button>
            </div>
            <form id="document-form" class="modal-form">
                <div class="modal-field">
                    <span>${window.t ? window.t('documents.name', 'Name') : 'Name'}</span>
                    <input id="document-name" required />
                </div>
                <div class="modal-field">
                    <span>${window.t ? window.t('documents.description', 'Description') : 'Description'}</span>
                    <textarea id="document-description" rows="3"></textarea>
                </div>
                <div class="field-grid">
                    <label class="modal-field">
                        <span>${window.t ? window.t('documents.visibleTo', 'Visible To') : 'Visible To'}</span>
                        <select id="document-visibility">
                            ${visibilityOptions.map(v => `<option value="${v.value}">${t(v.key, v.fallback)}</option>`).join('')}
                        </select>
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('documents.file', 'File') : 'File'}</span>
                        <input id="document-file" type="file" required />
                    </label>
                </div>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-document" type="button">${window.t ? window.t('common.cancel', 'Cancel') : 'Cancel'}</button>
                    <button class="primary-button" type="submit">${window.t ? window.t('documents.save', 'Upload') : 'Upload'}</button>
                </div>
            </form>
        </div>
    `;

    document.body.appendChild(modal);
    document.getElementById('close-document-modal').addEventListener('click', closeUploadModal);
    document.getElementById('cancel-document').addEventListener('click', closeUploadModal);
    document.getElementById('document-form').addEventListener('submit', handleDocumentUpload);
    modal.addEventListener('click', (e) => { if (e.target.id === 'document-modal') closeUploadModal(); });
}

function openUploadModal() {
    createUploadModal();
    document.getElementById('document-modal').classList.remove('hidden');
}

function closeUploadModal() {
    const modal = document.getElementById('document-modal');
    if (modal) modal.classList.add('hidden');
    const form = document.getElementById('document-form');
    if (form) form.reset();
}

async function handleDocumentUpload(e) {
    e.preventDefault();
    const name = document.getElementById('document-name').value.trim();
    const description = document.getElementById('document-description').value.trim();
    const visibility = document.getElementById('document-visibility').value;
    const fileInput = document.getElementById('document-file');
    const file = fileInput.files && fileInput.files[0];

    if (!file) return showToast(window.t ? window.t('documents.chooseFile', 'Please choose a file') : 'Please choose a file');

    const formData = new FormData();
    formData.append('name', name);
    formData.append('description', description);
    formData.append('visibleToRole', visibility);
    formData.append('file', file);

    try {
        const resp = await fetch('/api/documents/upload', {
            method: 'POST',
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` },
            body: formData
        });

        if (!resp.ok) {
            const err = await resp.json().catch(() => null);
            showToast(err?.message || (window.t ? window.t('documents.uploadFailed', 'Upload failed') : 'Upload failed'));
            return;
        }

        showToast(window.t ? window.t('documents.uploadedSuccess', 'Uploaded') : 'Uploaded');
        closeUploadModal();
        loadDocuments();
    } catch (err) {
        showToast(window.t ? window.t('documents.uploadFailed', 'Upload failed') : 'Upload failed');
    }
}

function renderDocuments() {
    if (!documentsSection) return;
    const container = documentsSection.querySelector('.documents-list');
    const searchTerm = documentsSearchQuery.trim().toLowerCase();
    const filteredDocuments = documentsList.filter((doc) => {
        if (!searchTerm) {
            return true;
        }

        return doc.name.toLowerCase().includes(searchTerm)
            || (doc.description || '').toLowerCase().includes(searchTerm)
            || (doc.visibleToRole || 'Everyone').toLowerCase().includes(searchTerm)
            || getUserName(doc.uploadedByUserId).toLowerCase().includes(searchTerm);
    });

    if (!filteredDocuments.length) {
        container.innerHTML = `<div class="empty-state">${window.t ? window.t('documents.empty', 'No documents match your search.') : 'No documents match your search.'}</div>`;
        return;
    }

    container.innerHTML = `
        <div class="documents-table-wrapper">
            <table class="documents-table">
                <thead>
                    <tr>
                        <th>${window.t ? window.t('documents.name', 'Name') : 'Name'}</th>
                        <th>${window.t ? window.t('documents.description', 'Description') : 'Description'}</th>
                        <th>${window.t ? window.t('documents.uploadedBy', 'Uploaded By') : 'Uploaded By'}</th>
                        <th>${window.t ? window.t('documents.visibleTo', 'Visible To') : 'Visible To'}</th>
                        <th>${window.t ? window.t('documents.uploaded', 'Uploaded') : 'Uploaded'}</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    ${filteredDocuments.map(doc => `
                        <tr>
                            <td><strong>${doc.name}</strong></td>
                            <td class="small-muted">${doc.description || '—'}</td>
                            <td>${getUserName(doc.uploadedByUserId)}</td>
                            <td><span class="badge">${doc.visibleToRole || 'Everyone'}</span></td>
                            <td class="small-muted">${formatDate(doc.createdAt)}</td>
                            <td>
                                <div class="doc-actions">
                                    <button class="mini-button" data-action="download" data-id="${doc.id}">${window.t ? window.t('documents.download', 'Download') : 'Download'}</button>
                                    ${canDeleteDocument(doc) ? `<button class="mini-button danger" data-action="delete" data-id="${doc.id}">${window.t ? window.t('documents.delete', 'Delete') : 'Delete'}</button>` : ''}
                                </div>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>
    `;

    container.querySelectorAll('[data-action="download"]').forEach(btn => {
        btn.addEventListener('click', () => downloadDocument(btn.dataset.id));
    });
    container.querySelectorAll('[data-action="delete"]').forEach(btn => {
        btn.addEventListener('click', () => deleteDocument(btn.dataset.id));
    });
}

async function loadDocuments() {
    if (!documentsSection) return;
    const container = documentsSection.querySelector('.documents-list');
container.innerHTML = `<div class="empty-state">${window.t ? window.t('documents.loading', 'Loading documents…') : 'Loading documents…'}</div>`;

    try {
        const [docsResp, usersResp] = await Promise.all([
            fetch('/api/documents', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }),
            fetch('/api/admin/users', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }).catch(() => ({ ok: false }))
        ]);

        if (!docsResp.ok) throw new Error('Unable to load documents');
        documentsList = await docsResp.json();
        if (usersResp.ok) users = await usersResp.json();
        renderDocuments();
    } catch (err) {
        container.innerHTML = `<div class="empty-state">${window.t ? window.t('documents.unable', 'Unable to load documents right now.') : 'Unable to load documents right now.'}</div>`;
    }
}

async function downloadDocument(id) {
    try {
        const resp = await fetch(`/api/documents/${id}/download`, {
            headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
        });
        if (!resp.ok) return showToast(window.t ? window.t('documents.downloadFailed', 'Download failed') : 'Download failed');
        const blob = await resp.blob();
        const disposition = resp.headers.get('content-disposition') || '';
        let filename = 'download.bin';
        const match = /filename="?([^;"\n]+)"?/.exec(disposition);
        if (match && match[1]) filename = match[1];

        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    } catch (err) {
        showToast(window.t ? window.t('documents.downloadFailed', 'Download failed') : 'Download failed');
    }
}

async function deleteDocument(id) {
    const doc = documentsList.find(d => d.id === id);
    if (!doc || !window.confirm(window.t ? window.t('documents.deleteConfirm', 'Delete {name}?').replace('{name}', doc.name) : `Delete ${doc.name}?`)) return;

    const resp = await fetch(`/api/documents/${id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
    });
    if (resp.ok) {
        showToast(window.t ? window.t('documents.deleted', 'Deleted') : 'Deleted');
        loadDocuments();
    }
}

function bindDocumentsSection() {
    if (!documentsSection) return;
    const searchInput = documentsSection.querySelector('.documents-search');
    if (searchInput) {
        searchInput.addEventListener('input', (event) => {
            documentsSearchQuery = event.target.value;
            renderDocuments();
        });
    }

    const addBtn = documentsSection.querySelector('.add-document-button');
    if (addBtn) {
        addBtn.removeAttribute('hidden');
        addBtn.addEventListener('click', () => {
            openUploadModal();
        });
    }
}

function initializeDocumentsSection() {
    bindDocumentsSection();
    loadDocuments();
}

if (document.readyState === 'loading') {
    window.addEventListener('DOMContentLoaded', initializeDocumentsSection);
} else {
    initializeDocumentsSection();
}

window.addEventListener('qore-language-changed', () => {
    renderDocuments();
});
})();
