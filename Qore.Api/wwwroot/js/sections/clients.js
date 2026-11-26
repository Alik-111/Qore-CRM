(function() {
const clientsSection = document.querySelector('[data-section-content="clients"]');

let clients = [];
let users = [];
let currentClientId = null;
let isEditing = false;

const t = (key, fallback) => window.t ? window.t(key, fallback) : fallback;

function formatDate(value) {
    if (!value) {
        return '—';
    }
    return new Date(value).toLocaleDateString();
}

function canManageClient(client, currentRole) {
    if (currentRole === 'Admin' || currentRole === 'Manager') {
        return true;
    }
    return client.assignedToUserId == null || client.assignedToUserId === window.currentUser?.storedUser?.id || client.assignedToUserId === window.currentUser?.raw?.nameid;
}

function createClientModal() {
    if (document.getElementById('client-modal')) {
        return;
    }

    const modal = document.createElement('div');
    modal.id = 'client-modal';
    modal.className = 'modal hidden';
    modal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header">
                <h3 id="modal-title">${window.t ? window.t('section.clients', 'Clients') : 'Clients'}</h3>
                <button class="icon-button" id="close-modal" type="button">×</button>
            </div>
            <form id="client-form" class="modal-form">
                <input type="hidden" id="client-id" />
                <div class="field-grid">
                    <label class="modal-field">
                        <span>${window.t ? window.t('clients.name', 'Name') : 'Name'}</span>
                        <input id="client-name" required />
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('clients.company', 'Company Name') : 'Company Name'}</span>
                        <input id="client-company" />
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('clients.email', 'Email') : 'Email'}</span>
                        <input id="client-email" type="email" />
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('clients.phone', 'Phone') : 'Phone'}</span>
                        <input id="client-phone" />
                    </label>
                </div>
                <label class="modal-field">
                    <span>${window.t ? window.t('clients.address', 'Address') : 'Address'}</span>
                    <textarea id="client-address" rows="2"></textarea>
                </label>
                <label class="modal-field">
                    <span>${window.t ? window.t('clients.notes', 'Notes') : 'Notes'}</span>
                    <textarea id="client-notes" rows="3"></textarea>
                </label>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-client" type="button">${window.t ? window.t('common.cancel', 'Cancel') : 'Cancel'}</button>
                    <button class="primary-button" type="submit">${window.t ? window.t('clients.save', 'Save Client') : 'Save Client'}</button>
                </div>
            </form>
        </div>
    `;

    document.body.appendChild(modal);
    document.getElementById('close-modal').addEventListener('click', closeClientModal);
    document.getElementById('cancel-client').addEventListener('click', closeClientModal);
    document.getElementById('client-form').addEventListener('submit', handleClientSubmit);
    modal.addEventListener('click', (event) => {
        if (event.target.id === 'client-modal') {
            closeClientModal();
        }
    });
}

function openClientModal() {
    createClientModal();
    document.getElementById('client-modal').classList.remove('hidden');
}

function closeClientModal() {
    const modal = document.getElementById('client-modal');
    if (modal) {
        modal.classList.add('hidden');
    }
    currentClientId = null;
    isEditing = false;
    const form = document.getElementById('client-form');
    if (form) {
        form.reset();
    }
}

function renderClients() {
    if (!clientsSection) {
        return;
    }

    const container = clientsSection.querySelector('.clients-list');
    const query = clientsSection.querySelector('.clients-search').value.toLowerCase();
    const currentRole = window.currentUser?.role || 'Employee';

    const filtered = clients.filter((client) => {
        const haystack = `${client.name} ${client.companyName}`.toLowerCase();
        return haystack.includes(query);
    });

    if (!filtered.length) {
        container.innerHTML = `<div class="empty-state">${window.t ? window.t('clients.empty', 'No clients match your search.') : 'No clients match your search.'}</div>`;
        return;
    }

    container.innerHTML = filtered.map((client) => {
        const canManage = canManageClient(client, currentRole);
        return `
            <article class="client-card" data-client-id="${client.id}">
                <div class="client-card-header">
                    <div>
                        <h4>${client.name}</h4>
                        <p>${client.companyName || (window.t ? window.t('clients.noCompany', 'No company') : 'No company')}</p>
                    </div>
                    <span class="badge">${client.assignedToUserId ? (window.t ? window.t('clients.assigned', 'Assigned') : 'Assigned') : (window.t ? window.t('clients.unassigned', 'Unassigned') : 'Unassigned')}</span>
                </div>
                <div class="client-card-body">
                    <div><strong>${window.t ? window.t('clients.email', 'Email') : 'Email'}</strong><span>${client.email || '—'}</span></div>
                    <div><strong>${window.t ? window.t('clients.phone', 'Phone') : 'Phone'}</strong><span>${client.phone || '—'}</span></div>
                    <div><strong>${window.t ? window.t('clients.updated', 'Updated') : 'Updated'}</strong><span>${formatDate(client.updatedAt)}</span></div>
                </div>
                <div class="client-card-actions">
                    <button class="mini-button" data-action="view" data-client-id="${client.id}">${window.t ? window.t('clients.view', 'View') : 'View'}</button>
                    ${canManage ? `<button class="mini-button secondary" data-action="edit" data-client-id="${client.id}">${window.t ? window.t('clients.edit', 'Edit') : 'Edit'}</button>` : ''}
                    ${canManage ? `<button class="mini-button danger" data-action="delete" data-client-id="${client.id}">${window.t ? window.t('clients.delete', 'Delete') : 'Delete'}</button>` : ''}
                </div>
            </article>
        `;
    }).join('');

    container.querySelectorAll('[data-action]').forEach((button) => {
        button.addEventListener('click', (event) => {
            event.stopPropagation();
            const action = button.dataset.action;
            const id = button.dataset.clientId;
            if (action === 'view') {
                openDetail(id);
            } else if (action === 'edit') {
                editClient(id);
            } else if (action === 'delete') {
                deleteClient(id);
            }
        });
    });

    container.querySelectorAll('.client-card').forEach((card) => {
        card.addEventListener('click', () => openDetail(card.dataset.clientId));
    });
}

async function loadClients() {
    if (!clientsSection) {
        return;
    }

    const list = clientsSection.querySelector('.clients-list');
    list.innerHTML = `<div class="empty-state">${window.t ? window.t('clients.loading', 'Loading clients…') : 'Loading clients…'}</div>`;

    try {
        const [clientsResponse, usersResponse] = await Promise.all([
            fetch('/api/clients', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }),
            fetch('/api/admin/users', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }).catch(() => ({ ok: false }))
        ]);

        if (!clientsResponse.ok) {
            throw new Error('Unable to load clients');
        }

        clients = await clientsResponse.json();
        if (usersResponse.ok) {
            users = await usersResponse.json();
        }
        renderClients();
    } catch (error) {
        list.innerHTML = `<div class="empty-state">${window.t ? window.t('clients.unable', 'Unable to load clients right now.') : 'Unable to load clients right now.'}</div>`;
    }
}

function openDetail(clientId) {
    const client = clients.find((item) => item.id === clientId);
    if (!client) {
        return;
    }

    const currentRole = window.currentUser?.role || 'Employee';
    const canManage = canManageClient(client, currentRole);
    const detail = document.createElement('div');
    detail.className = 'detail-card';
    detail.innerHTML = `
        <h4>${client.name}</h4>
        <p>${client.companyName || t('clients.noCompany', 'No company')}</p>
        <div class="detail-grid">
            <div><strong>${window.t ? window.t('clients.email', 'Email') : 'Email'}</strong><span>${client.email || '—'}</span></div>
            <div><strong>${window.t ? window.t('clients.phone', 'Phone') : 'Phone'}</strong><span>${client.phone || '—'}</span></div>
            <div><strong>${window.t ? window.t('clients.address', 'Address') : 'Address'}</strong><span>${client.address || '—'}</span></div>
            <div><strong>${window.t ? window.t('clients.notes', 'Notes') : 'Notes'}</strong><span>${client.notes || '—'}</span></div>
            <div><strong>${window.t ? window.t('clients.assignedTo', 'Assigned To') : 'Assigned To'}</strong><span>${users.find((u) => u.id === client.assignedToUserId)?.fullName || (window.t ? window.t('clients.unassigned', 'Unassigned') : 'Unassigned')}</span></div>
            <div><strong>${window.t ? window.t('clients.lastUpdated', 'Last Updated') : 'Last Updated'}</strong><span>${formatDate(client.updatedAt)}</span></div>
        </div>
        <div class="modal-actions">
            ${canManage ? `<button class="mini-button secondary" data-action="edit">${window.t ? window.t('clients.edit', 'Edit') : 'Edit'}</button>` : ''}
            ${canManage ? `<button class="mini-button danger" data-action="delete">${window.t ? window.t('clients.delete', 'Delete') : 'Delete'}</button>` : ''}
            <button class="secondary-button" data-action="close">${window.t ? window.t('clients.close', 'Close') : 'Close'}</button>
        </div>
    `;

    const wrapper = document.createElement('div');
    wrapper.className = 'modal hidden';
    wrapper.id = 'detail-modal';
    wrapper.innerHTML = '<div class="modal-card"></div>';
    wrapper.querySelector('.modal-card').appendChild(detail);
    document.body.appendChild(wrapper);
    wrapper.classList.remove('hidden');

    wrapper.querySelector('[data-action="close"]').addEventListener('click', () => wrapper.remove());
    wrapper.querySelector('[data-action="edit"]')?.addEventListener('click', () => {
        wrapper.remove();
        editClient(client.id);
    });
    wrapper.querySelector('[data-action="delete"]')?.addEventListener('click', () => {
        wrapper.remove();
        deleteClient(client.id);
    });
}

async function handleClientSubmit(event) {
    event.preventDefault();
    const payload = {
        name: document.getElementById('client-name').value.trim(),
        companyName: document.getElementById('client-company').value.trim(),
        email: document.getElementById('client-email').value.trim(),
        phone: document.getElementById('client-phone').value.trim(),
        address: document.getElementById('client-address').value.trim(),
        notes: document.getElementById('client-notes').value.trim()
    };

    const method = isEditing ? 'PUT' : 'POST';
    const url = isEditing ? `/api/clients/${currentClientId}` : '/api/clients';

    const response = await fetch(url, {
        method,
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
        },
        body: JSON.stringify(payload)
    });

    if (response.ok) {
        closeClientModal();
        loadClients();
    }
}

function editClient(clientId) {
    const client = clients.find((item) => item.id === clientId);
    if (!client) {
        return;
    }

    openClientModal();
    isEditing = true;
    currentClientId = clientId;
    document.getElementById('modal-title').textContent = window.t ? window.t('clients.modalTitleEdit', 'Edit Client') : 'Edit Client';
    document.getElementById('client-id').value = client.id;
    document.getElementById('client-name').value = client.name;
    document.getElementById('client-company').value = client.companyName;
    document.getElementById('client-email').value = client.email;
    document.getElementById('client-phone').value = client.phone;
    document.getElementById('client-address').value = client.address;
    document.getElementById('client-notes').value = client.notes;
}

async function deleteClient(clientId) {
    const client = clients.find((item) => item.id === clientId);
    if (!client || !window.confirm(t('clients.deleteConfirm', 'Delete {name}?').replace('{name}', client.name))) {
        return;
    }

    const response = await fetch(`/api/clients/${clientId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
    });

    if (response.ok) {
        loadClients();
    }
}

function bindClientsSection() {
    if (!clientsSection) {
        return;
    }

    const searchInput = clientsSection.querySelector('.clients-search');
    if (searchInput) {
        searchInput.addEventListener('input', renderClients);
    }

    const addButton = clientsSection.querySelector('.add-client-button');
    if (addButton) {
        addButton.addEventListener('click', () => {
            isEditing = false;
            currentClientId = null;
            openClientModal();
            const title = document.getElementById('modal-title');
            if (title) {
                title.textContent = window.t ? window.t('clients.modalTitleCreate', 'Add Client') : 'Add Client';
            }
        });
    }
}

window.addEventListener('DOMContentLoaded', () => {
    bindClientsSection();
    loadClients();
});

window.addEventListener('qore-language-changed', () => {
    renderClients();
});
})();
