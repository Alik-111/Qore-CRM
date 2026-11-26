(function() {
    const pipelineSection = document.querySelector('[data-section-content="pipeline"]');

    let opportunities = [];
    let clients = [];
    let users = [];
    let activeStatusFilter = 'All';
    let pipelineSearchQuery = '';

    const statusColumns = ['New', 'Contacted', 'Proposal', 'Negotiation', 'Won', 'Lost'];

    function getStatusLabel(status) {
        const translationKey = {
            New: 'pipeline.filterNew',
            Contacted: 'pipeline.filterContacted',
            Proposal: 'pipeline.filterProposal',
            Negotiation: 'pipeline.filterNegotiation',
            Won: 'pipeline.filterWon',
            Lost: 'pipeline.filterLost'
        }[status];

        return window.t ? window.t(translationKey || 'pipeline.filterAll', status) : status;
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value || 0);
    }

function getStatusClass(status) {
    return status.toLowerCase().replace(/ /g, '-');
}

function getClientName(id) {
    const client = clients.find((item) => item.id === id);
    return client ? client.name : (window.t ? window.t('pipeline.unknownClient', 'Unknown Client') : 'Unknown Client');
}

function getUserName(id) {
    const user = users.find((item) => item.id === id);
    return user ? user.fullName : (window.t ? window.t('pipeline.unassigned', 'Unassigned') : 'Unassigned');
}

function canManageOpportunity(opportunity) {
    const role = window.currentUser?.role || 'Employee';
    if (role === 'Admin' || role === 'Manager') {
        return true;
    }
    return opportunity.assignedToUserId === window.currentUser?.storedUser?.id || opportunity.assignedToUserId === window.currentUser?.raw?.nameid;
}

function createOpportunityModal() {
    if (document.getElementById('opportunity-modal')) {
        return;
    }

    const modal = document.createElement('div');
    modal.id = 'opportunity-modal';
    modal.className = 'modal hidden';
    modal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header">
                <h3 id="opportunity-modal-title">${window.t ? window.t('pipeline.modalTitleCreate', 'Add Opportunity') : 'Add Opportunity'}</h3>
                <button class="icon-button" id="close-opportunity-modal" type="button">×</button>
            </div>
            <form id="opportunity-form" class="modal-form">
                <input type="hidden" id="opportunity-id" />
                <div class="field-grid">
                    <label class="modal-field">
                        <span id="opportunity-title-label">${window.t ? window.t('pipeline.title', 'Title') : 'Title'}</span>
                        <input id="opportunity-title" required />
                    </label>
                    <label class="modal-field">
                        <span id="opportunity-client-label">${window.t ? window.t('pipeline.client', 'Client') : 'Client'}</span>
                        <select id="opportunity-client"></select>
                    </label>
                    <label class="modal-field">
                        <span id="opportunity-value-label">${window.t ? window.t('pipeline.value', 'Value') : 'Value'}</span>
                        <input id="opportunity-value" type="number" min="1" step="1" required />
                    </label>
                    <label class="modal-field">
                        <span id="opportunity-assigned-label">${window.t ? window.t('pipeline.assignedTo', 'Assigned To') : 'Assigned To'}</span>
                        <select id="opportunity-assigned" required></select>
                    </label>
                    <label class="modal-field">
                        <span id="opportunity-status-label">${window.t ? window.t('pipeline.status', 'Status') : 'Status'}</span>
                        <select id="opportunity-status">
                            ${statusColumns.map((status) => `<option value="${status}">${getStatusLabel(status)}</option>`).join('')}
                        </select>
                    </label>
                </div>
                <label class="modal-field">
                    <span id="opportunity-description-label">${window.t ? window.t('pipeline.description', 'Description') : 'Description'}</span>
                    <textarea id="opportunity-description" rows="4"></textarea>
                </label>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-opportunity" type="button">${window.t ? window.t('common.cancel', 'Cancel') : 'Cancel'}</button>
                    <button class="primary-button" id="save-opportunity" type="submit">${window.t ? window.t('pipeline.save', 'Save') : 'Save'}</button>
                </div>
            </form>
        </div>
    `;

    document.body.appendChild(modal);
    document.getElementById('close-opportunity-modal').addEventListener('click', closeOpportunityModal);
    document.getElementById('cancel-opportunity').addEventListener('click', closeOpportunityModal);
    document.getElementById('opportunity-form').addEventListener('submit', handleOpportunitySubmit);
    modal.addEventListener('click', (event) => {
        if (event.target.id === 'opportunity-modal') {
            closeOpportunityModal();
        }
    });
}

function openOpportunityModal() {
    createOpportunityModal();
    populatePickers();
    document.getElementById('opportunity-modal').classList.remove('hidden');
}

function closeOpportunityModal() {
    const modal = document.getElementById('opportunity-modal');
    if (modal) {
        modal.classList.add('hidden');
    }
    const form = document.getElementById('opportunity-form');
    if (form) {
        form.reset();
    }
    const idInput = document.getElementById('opportunity-id');
    if (idInput) {
        idInput.value = '';
    }
}

function populatePickers() {
    const assignedSelect = document.getElementById('opportunity-assigned');
    const clientSelect = document.getElementById('opportunity-client');

    if (assignedSelect) {
        assignedSelect.innerHTML = '<option value="">Unassigned</option>';
        users.forEach((user) => {
            const option = document.createElement('option');
            option.value = user.id;
            option.textContent = `${user.fullName} (${user.role})`;
            assignedSelect.appendChild(option);
        });
    }

    if (clientSelect) {
        clientSelect.innerHTML = '<option value="">No client</option>';
        clients.forEach((client) => {
            const option = document.createElement('option');
            option.value = client.id;
            option.textContent = client.name;
            clientSelect.appendChild(option);
        });
    }
}

function syncPipelineSectionLabels() {
    if (!pipelineSection) {
        return;
    }

    const searchInput = pipelineSection.querySelector('.pipeline-search');
    if (searchInput) {
        searchInput.placeholder = window.t ? window.t('pipeline.searchPlaceholder', 'Search opportunities') : 'Search opportunities';
    }

    const filterSelect = pipelineSection.querySelector('.pipeline-filter');
    if (filterSelect) {
        Array.from(filterSelect.options).forEach((option) => {
            const key = option.getAttribute('data-i18n');
            if (key) {
                option.textContent = window.t ? window.t(key, option.textContent) : option.textContent;
            }
        });
    }

    const addButton = pipelineSection.querySelector('.add-opportunity-button');
    if (addButton) {
        addButton.textContent = window.t ? window.t('pipeline.addOpportunity', 'Add Opportunity') : 'Add Opportunity';
    }

    const summaryPill = pipelineSection.querySelector('.pipeline-summary .pill');
    if (summaryPill) {
        summaryPill.textContent = window.t ? window.t('pipeline.pipelineValue', 'Pipeline Value') : 'Pipeline Value';
    }
}

function refreshOpportunityModal() {
    const existingModal = document.getElementById('opportunity-modal');
    if (!existingModal) {
        return;
    }

    const title = document.getElementById('opportunity-modal-title');
    if (title) {
        const modalId = document.getElementById('opportunity-id')?.value || '';
        title.textContent = modalId
            ? (window.t ? window.t('pipeline.modalTitleEdit', 'Edit Opportunity') : 'Edit Opportunity')
            : (window.t ? window.t('pipeline.modalTitleCreate', 'Add Opportunity') : 'Add Opportunity');
    }

    const labels = [
        ['opportunity-title-label', 'pipeline.title', 'Title'],
        ['opportunity-client-label', 'pipeline.client', 'Client'],
        ['opportunity-value-label', 'pipeline.value', 'Value'],
        ['opportunity-assigned-label', 'pipeline.assignedTo', 'Assigned To'],
        ['opportunity-status-label', 'pipeline.status', 'Status'],
        ['opportunity-description-label', 'pipeline.description', 'Description']
    ];

    labels.forEach(([id, key, fallback]) => {
        const label = document.getElementById(id);
        if (label) {
            label.textContent = window.t ? window.t(key, fallback) : fallback;
        }
    });

    const cancelButton = document.getElementById('cancel-opportunity');
    if (cancelButton) {
        cancelButton.textContent = window.t ? window.t('common.cancel', 'Cancel') : 'Cancel';
    }

    const saveButton = document.getElementById('save-opportunity');
    if (saveButton) {
        saveButton.textContent = window.t ? window.t('pipeline.save', 'Save') : 'Save';
    }

    const statusSelect = document.getElementById('opportunity-status');
    if (statusSelect) {
        const currentValue = statusSelect.value;
        statusSelect.innerHTML = statusColumns.map((status) => `<option value="${status}" ${status === currentValue ? 'selected' : ''}>${getStatusLabel(status)}</option>`).join('');
        if (!statusColumns.includes(currentValue)) {
            statusSelect.value = statusColumns[0];
        }
    }
}

function renderBoard() {
    if (!pipelineSection) {
        return;
    }

    const board = pipelineSection.querySelector('.pipeline-board');
    const totalValueEl = pipelineSection.querySelector('.pipeline-total');

    const searchTerm = pipelineSearchQuery.trim().toLowerCase();
    const filtered = opportunities.filter((opportunity) => {
        const matchesStatus = activeStatusFilter === 'All' || opportunity.status === activeStatusFilter;
        if (!matchesStatus) {
            return false;
        }

        if (!searchTerm) {
            return true;
        }

        const clientName = getClientName(opportunity.clientId).toLowerCase();
        const assignedName = getUserName(opportunity.assignedToUserId).toLowerCase();
        return opportunity.title.toLowerCase().includes(searchTerm)
            || (opportunity.description || '').toLowerCase().includes(searchTerm)
            || clientName.includes(searchTerm)
            || assignedName.includes(searchTerm)
            || opportunity.status.toLowerCase().includes(searchTerm);
    });

    const visibleValue = filtered.reduce((sum, item) => sum + (item.value || 0), 0);
    totalValueEl.textContent = formatCurrency(visibleValue);

    board.innerHTML = '';
    statusColumns.forEach((status) => {
        const column = document.createElement('section');
        column.className = `pipeline-column ${getStatusClass(status)}`;
        const heading = document.createElement('div');
        heading.className = 'pipeline-column-header';
        heading.innerHTML = `<h4>${getStatusLabel(status)}</h4><span>${filtered.filter((item) => item.status === status).length}</span>`;

        const list = document.createElement('div');
        list.className = 'pipeline-list';

        const cards = filtered.filter((item) => item.status === status);
        cards.forEach((opportunity) => {
            const card = document.createElement('article');
            card.className = 'opportunity-card';
            card.innerHTML = `
                <div class="opportunity-card-head">
                    <strong>${opportunity.title}</strong>
                    <span class="badge">${formatCurrency(opportunity.value)}</span>
                </div>
                <div class="small-muted">${getClientName(opportunity.clientId)}</div>
                <div class="small-muted">Assigned to ${getUserName(opportunity.assignedToUserId)}</div>
                <div class="opportunity-card-footer">
                    <select class="status-select" data-id="${opportunity.id}">
                        ${statusColumns.map((option) => `<option value="${option}" ${option === opportunity.status ? 'selected' : ''}>${getStatusLabel(option)}</option>`).join('')}
                    </select>
                    <div class="opportunity-actions">
                        <button class="mini-button" data-action="edit" data-id="${opportunity.id}">${window.t ? window.t('clients.edit', 'Edit') : 'Edit'}</button>
                        <button class="mini-button danger" data-action="delete" data-id="${opportunity.id}">${window.t ? window.t('clients.delete', 'Delete') : 'Delete'}</button>
                    </div>
                </div>
            `;
            list.appendChild(card);
        });

        column.appendChild(heading);
        column.appendChild(list);
        board.appendChild(column);
    });

    board.querySelectorAll('.status-select').forEach((select) => {
        select.addEventListener('change', (event) => updateStatus(event.target.dataset.id, event.target.value));
    });

    board.querySelectorAll('[data-action="edit"]').forEach((button) => {
        button.addEventListener('click', () => editOpportunity(button.dataset.id));
    });

    board.querySelectorAll('[data-action="delete"]').forEach((button) => {
        button.addEventListener('click', () => deleteOpportunity(button.dataset.id));
    });
}

async function loadPipeline() {
    if (!pipelineSection) {
        return;
    }

    const board = pipelineSection.querySelector('.pipeline-board');
    board.innerHTML = `<div class="empty-state">${window.t ? window.t('pipeline.loading', 'Loading opportunities…') : 'Loading opportunities…'}</div>`;

    try {
        const [opportunitiesResponse, clientsResponse, usersResponse] = await Promise.all([
            fetch('/api/opportunities', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }),
            fetch('/api/clients', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }).catch(() => ({ ok: false })),
            fetch('/api/admin/users', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }).catch(() => ({ ok: false }))
        ]);

        if (!opportunitiesResponse.ok) {
            throw new Error('Unable to load opportunities');
        }

        opportunities = await opportunitiesResponse.json();
        clients = clientsResponse.ok ? await clientsResponse.json() : [];
        users = usersResponse.ok ? await usersResponse.json() : [];
        populatePickers();
        renderBoard();
    } catch (error) {
        board.innerHTML = `<div class="empty-state">${window.t ? window.t('pipeline.unable', 'Unable to load opportunities right now.') : 'Unable to load opportunities right now.'}</div>`;
    }
}

async function handleOpportunitySubmit(event) {
    event.preventDefault();
    const id = document.getElementById('opportunity-id').value;
    const assignedToUserId = document.getElementById('opportunity-assigned').value || users[0]?.id || window.currentUser?.storedUser?.id || window.currentUser?.raw?.nameid || '';

    if (!assignedToUserId) {
        window.alert(window.t ? window.t('pipeline.selectAssignee', 'Please select an assignee before saving.') : 'Please select an assignee before saving.');
        return;
    }

    const clientIdValue = document.getElementById('opportunity-client').value;
    const payload = {
        title: document.getElementById('opportunity-title').value.trim(),
        clientId: clientIdValue ? clientIdValue : null,
        value: Number(document.getElementById('opportunity-value').value || 0),
        assignedToUserId,
        status: document.getElementById('opportunity-status').value,
        description: document.getElementById('opportunity-description').value.trim()
    };

    const response = await fetch(id ? `/api/opportunities/${id}` : '/api/opportunities', {
        method: id ? 'PUT' : 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
        },
        body: JSON.stringify(payload)
    });

    if (response.ok) {
        closeOpportunityModal();
        await loadPipeline();
        if (window.loadDashboard) {
            await window.loadDashboard();
        }
        return;
    }

    const errorText = await response.text();
    console.error('Opportunity save failed:', response.status, errorText);
    window.alert(window.t ? window.t('pipeline.saveFailed', 'Unable to save opportunity. See console for details.') : 'Unable to save opportunity. See console for details.');
}

async function updateStatus(id, status) {
    const response = await fetch(`/api/opportunities/${id}/status`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
        },
        body: JSON.stringify({ status })
    });

    if (response.ok) {
        await loadPipeline();
        if (window.loadDashboard) {
            await window.loadDashboard();
        }
    }
}

function editOpportunity(id) {
    const opportunity = opportunities.find((item) => item.id === id);
    if (!opportunity) {
        return;
    }

    openOpportunityModal();
    document.getElementById('opportunity-modal-title').textContent = window.t ? window.t('pipeline.modalTitleEdit', 'Edit Opportunity') : 'Edit Opportunity';
    document.getElementById('opportunity-id').value = opportunity.id;
    document.getElementById('opportunity-title').value = opportunity.title;
    document.getElementById('opportunity-value').value = opportunity.value;
    document.getElementById('opportunity-client').value = opportunity.clientId || '';
    document.getElementById('opportunity-assigned').value = opportunity.assignedToUserId || '';
    document.getElementById('opportunity-status').value = opportunity.status;
    document.getElementById('opportunity-description').value = opportunity.description;
}

async function deleteOpportunity(id) {
    const opportunity = opportunities.find((item) => item.id === id);
    if (!opportunity) {
        return;
    }

    const confirmMessage = window.t ? window.t('pipeline.deleteConfirm', 'Delete {name}?').replace('{name}', opportunity.title) : `Delete ${opportunity.title}?`;
    if (!window.confirm(confirmMessage)) {
        return;
    }

    const response = await fetch(`/api/opportunities/${id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
    });

    if (response.ok) {
        loadPipeline();
    }
}

function bindPipelineSection() {
    if (!pipelineSection) {
        return;
    }

    const filterSelect = pipelineSection.querySelector('.pipeline-filter');
    if (filterSelect) {
        filterSelect.addEventListener('change', (event) => {
            activeStatusFilter = event.target.value;
            renderBoard();
        });
    }

    const searchInput = pipelineSection.querySelector('.pipeline-search');
    if (searchInput) {
        searchInput.addEventListener('input', (event) => {
            pipelineSearchQuery = event.target.value;
            renderBoard();
        });
    }

    const addButton = pipelineSection.querySelector('.add-opportunity-button');
    if (addButton) {
        addButton.addEventListener('click', () => {
            openOpportunityModal();
            const title = document.getElementById('opportunity-modal-title');
            if (title) {
                title.textContent = window.t ? window.t('pipeline.modalTitleCreate', 'Add Opportunity') : 'Add Opportunity';
            }
            const idInput = document.getElementById('opportunity-id');
            if (idInput) {
                idInput.value = '';
            }
        });
    }
}

function initializePipelineSection() {
    bindPipelineSection();
    loadPipeline();
}

if (document.readyState === 'loading') {
    window.addEventListener('DOMContentLoaded', initializePipelineSection);
} else {
    initializePipelineSection();
}

window.addEventListener('qore-language-changed', () => {
    window.qoreI18n?.applyTranslations(document);
    syncPipelineSectionLabels();
    renderBoard();
    refreshOpportunityModal();
});
})();
