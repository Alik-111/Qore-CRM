(function() {
    const tasksSection = document.querySelector('[data-section-content="tasks"]');

    let tasks = [];
    let users = [];
    let assigneeFilter = 'All';
    let taskModal = null;

    const taskStatuses = [
        { value: 'New', label: 'New' },
        { value: 'InProgress', label: 'In Progress' },
        { value: 'Completed', label: 'Completed' }
    ];
    const taskPriorities = ['Low', 'Medium', 'High'];
    const t = (key, fallback) => (window.t ? window.t(key, fallback) : fallback);

    function normalizeTaskStatus(status) {
        if (status === 'In Progress') {
            return 'InProgress';
        }

        return status || 'New';
    }

    function getTaskStatusLabel(status) {
        switch (normalizeTaskStatus(status)) {
            case 'InProgress':
                return 'In Progress';
            case 'Completed':
                return 'Completed';
            default:
                return 'New';
        }
    }

    function formatDate(value) {
    if (!value) {
        return '—';
    }
    return new Date(value).toLocaleDateString();
}

function getUserName(id) {
    const user = users.find((item) => item.id === id);
    return user ? user.fullName : 'Unassigned';
}

function getBadgeClass(status) {
    switch (normalizeTaskStatus(status)) {
        case 'Completed':
            return 'badge completed';
        case 'InProgress':
            return 'badge in-progress';
        default:
            return 'badge new';
    }
}

function getPriorityClass(priority) {
    switch (priority) {
        case 'High':
            return 'badge high';
        case 'Low':
            return 'badge low';
        default:
            return 'badge medium';
    }
}

function isOverdue(task) {
    if (!task.dueDate || task.status === 'Completed') {
        return false;
    }
    return new Date(task.dueDate) < new Date();
}

function canManageTasks() {
    const role = window.currentUser?.role || 'Employee';
    return role === 'Admin' || role === 'Manager';
}

function canEditTask(task) {
    if (canManageTasks()) {
        return true;
    }
    return task.assignedToUserId === window.currentUser?.storedUser?.id || task.assignedToUserId === window.currentUser?.raw?.nameid;
}

function showToast(message) {
    let toast = document.getElementById('task-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'task-toast';
        toast.className = 'toast';
        document.body.appendChild(toast);
    }

    toast.textContent = message;
    toast.classList.add('show');
    window.clearTimeout(showToast.timeout);
    showToast.timeout = window.setTimeout(() => toast.classList.remove('show'), 1800);
}

function createModal() {
    if (document.getElementById('task-modal')) {
        return;
    }

    taskModal = document.createElement('div');
    taskModal.id = 'task-modal';
    taskModal.className = 'modal hidden';
    taskModal.innerHTML = `
        <div class="modal-card">
            <div class="modal-header">
                <h3 id="task-modal-title">${window.t ? window.t('section.tasks', 'Tasks') : 'Tasks'}</h3>
                <button class="icon-button" id="close-task-modal" type="button">×</button>
            </div>
            <form id="task-form" class="modal-form">
                <input type="hidden" id="task-id" />
                <div class="field-grid">
                    <label class="modal-field">
                        <span>${window.t ? window.t('tasks.title', 'Title') : 'Title'}</span>
                        <input id="task-title" required />
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('tasks.assignTo', 'Assign To') : 'Assign To'}</span>
                        <select id="task-assigned"></select>
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('tasks.priorityLabel', 'Priority') : 'Priority'}</span>
                        <select id="task-priority">
                            ${taskPriorities.map((priority) => `<option value="${priority}">${priority}</option>`).join('')}
                        </select>
                    </label>
                    <label class="modal-field">
                        <span>${window.t ? window.t('tasks.dueDate', 'Due Date') : 'Due Date'}</span>
                        <input id="task-due-date" type="date" />
                    </label>
                    <label class="modal-field" id="task-status-field">
                        <span>${window.t ? window.t('tasks.status', 'Status') : 'Status'}</span>
                        <select id="task-status">
                            ${taskStatuses.map((status) => `<option value="${status.value}">${status.label}</option>`).join('')}
                        </select>
                    </label>
                </div>
                <label class="modal-field">
                    <span>${window.t ? window.t('tasks.description', 'Description') : 'Description'}</span>
                    <textarea id="task-description" rows="4"></textarea>
                </label>
                <div class="modal-actions">
                    <button class="secondary-button" id="cancel-task" type="button">${window.t ? window.t('common.cancel', 'Cancel') : 'Cancel'}</button>
                    <button class="primary-button" type="submit">${window.t ? window.t('tasks.save', 'Save Task') : 'Save Task'}</button>
                </div>
            </form>
        </div>
    `;

    document.body.appendChild(taskModal);
    document.getElementById('close-task-modal').addEventListener('click', closeTaskModal);
    document.getElementById('cancel-task').addEventListener('click', closeTaskModal);
    document.getElementById('task-form').addEventListener('submit', handleTaskSubmit);
    taskModal.addEventListener('click', (event) => {
        if (event.target.id === 'task-modal') {
            closeTaskModal();
        }
    });
}

function openTaskModal() {
    createModal();
    populateAssignees();
    taskModal.classList.remove('hidden');
}

function closeTaskModal() {
    if (!taskModal) {
        return;
    }
    taskModal.classList.add('hidden');
    const form = document.getElementById('task-form');
    if (form) {
        form.reset();
    }
    const taskIdInput = document.getElementById('task-id');
    if (taskIdInput) {
        taskIdInput.value = '';
    }
    const statusField = document.getElementById('task-status-field');
    if (statusField) {
        statusField.style.display = 'flex';
    }
}

function populateAssignees() {
    const select = document.getElementById('task-assigned');
    if (!select) {
        return;
    }

    select.innerHTML = '';
    const currentUserEntry = window.currentUser?.storedUser || window.currentUser?.raw;
    if (currentUserEntry && !users.length) {
        const option = document.createElement('option');
        option.value = currentUserEntry.id || currentUserEntry.nameid || '';
        option.textContent = currentUserEntry.fullName || currentUserEntry.name || 'Me';
        select.appendChild(option);
        return;
    }

    users.forEach((user) => {
        const option = document.createElement('option');
        option.value = user.id;
        option.textContent = `${user.fullName} (${user.role})`;
        select.appendChild(option);
    });
}

function renderTasks() {
    if (!tasksSection) {
        return;
    }

    const container = tasksSection.querySelector('.tasks-list');
    const currentRole = window.currentUser?.role || 'Employee';
    const canManage = canManageTasks();

    const filtered = tasks.filter((task) => {
        if (assigneeFilter !== 'All' && task.assignedToUserId !== assigneeFilter) {
            return false;
        }
        return true;
    });

    if (!filtered.length) {
        container.innerHTML = `<div class="empty-state">${t('tasks.empty', 'No tasks match the current view.')}</div>`;
        return;
    }

    container.innerHTML = `
        <div class="tasks-table-wrapper">
            <table class="tasks-table">
                <thead>
                    <tr>
                        <th>${t('tasks.title', 'Title')}</th>
                        <th>${t('tasks.assignedTo', 'Assigned To')}</th>
                        <th class="task-status-column">${t('tasks.status', 'Status')}</th>
                        <th>${t('tasks.priority', 'Priority')}</th>
                        <th>${t('tasks.dueDate', 'Due Date')}</th>
                        <th>${t('tasks.actions', 'Actions')}</th>
                    </tr>
                </thead>
                <tbody>
                    ${filtered.map((task) => {
                        const overdue = isOverdue(task);
                        const editable = canEditTask(task);
                        const canUpdateStatus = editable && task.assignedToUserId === (window.currentUser?.storedUser?.id || window.currentUser?.raw?.nameid) || canManage;
                        return `
                            <tr class="task-row ${overdue ? 'overdue' : ''}">
                                <td data-label="${t('tasks.title', 'Title')}">
                                    <strong>${task.title}</strong>
                                    <div class="small-muted">${task.description || (window.t ? window.t('tasks.noDescription', 'No description') : 'No description')}</div>
                                </td>
                                <td data-label="${t('tasks.assignedTo', 'Assigned To')}">${getUserName(task.assignedToUserId)}</td>
                                <td class="task-status-column" data-label="${t('tasks.status', 'Status')}"><span class="${getBadgeClass(task.status)}">${getTaskStatusLabel(task.status)}</span></td>
                                <td data-label="${t('tasks.priority', 'Priority')}"><span class="${getPriorityClass(task.priority)}">${task.priority}</span></td>
                                <td data-label="${t('tasks.dueDate', 'Due Date')}">${formatDate(task.dueDate)}</td>
                                <td data-label="${t('tasks.actions', 'Actions')}">
                                    <div class="task-actions">
                                        ${canManage ? `<button class="mini-button" data-action="edit" data-id="${task.id}">${t('tasks.edit', 'Edit')}</button>` : ''}
                                        ${canManage ? `<button class="mini-button danger" data-action="delete" data-id="${task.id}">${t('tasks.delete', 'Delete')}</button>` : ''}
                                        <select class="status-select" data-action="status" data-id="${task.id}" ${editable ? '' : 'disabled'}>
                                            ${taskStatuses.map((status) => `<option value="${status.value}" ${status.value === normalizeTaskStatus(task.status) ? 'selected' : ''}>${status.label}</option>`).join('')}
                                        </select>
                                    </div>
                                </td>
                            </tr>
                        `;
                    }).join('')}
                </tbody>
            </table>
        </div>
    `;

    container.querySelectorAll('[data-action="status"]').forEach((select) => {
        select.addEventListener('change', (event) => {
            updateTaskStatus(event.target.dataset.id, event.target.value);
        });
    });

    container.querySelectorAll('[data-action="edit"]').forEach((button) => {
        button.addEventListener('click', () => editTask(button.dataset.id));
    });

    container.querySelectorAll('[data-action="delete"]').forEach((button) => {
        button.addEventListener('click', () => deleteTask(button.dataset.id));
    });
}

async function loadTasks() {
    if (!tasksSection) {
        return;
    }

    const container = tasksSection.querySelector('.tasks-list');
    container.innerHTML = `<div class="empty-state">${window.t ? window.t('tasks.loading', 'Loading tasks…') : 'Loading tasks…'}</div>`;

    try {
        const [tasksResponse, usersResponse] = await Promise.all([
            fetch('/api/tasks', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }),
            fetch('/api/admin/users', { headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` } }).catch(() => ({ ok: false }))
        ]);

        if (!tasksResponse.ok) {
            throw new Error('Unable to load tasks');
        }

        tasks = await tasksResponse.json();
        if (usersResponse.ok) {
            users = await usersResponse.json();
        } else if (window.currentUser?.storedUser) {
            users = [{ id: window.currentUser.storedUser.id, fullName: window.currentUser.storedUser.fullName, role: window.currentUser.role }];
        }

        populateAssigneeFilter();
        renderTasks();
    } catch (error) {
        container.innerHTML = `<div class="empty-state">${window.t ? window.t('tasks.unable', 'Unable to load tasks right now.') : 'Unable to load tasks right now.'}</div>`;
    }
}

function populateAssigneeFilter() {
    const filter = tasksSection?.querySelector('.tasks-assignee-filter');
    if (!filter) {
        return;
    }

    filter.innerHTML = `<option value="All">${window.t ? window.t('tasks.allAssignees', 'All assignees') : 'All assignees'}</option>`;
    users.forEach((user) => {
        const option = document.createElement('option');
        option.value = user.id;
        option.textContent = user.fullName;
        filter.appendChild(option);
    });
}

function editTask(taskId) {
    const task = tasks.find((item) => item.id === taskId);
    if (!task) {
        return;
    }

    openTaskModal();
    document.getElementById('task-modal-title').textContent = window.t ? window.t('tasks.modalTitleEdit', 'Edit Task') : 'Edit Task';
    document.getElementById('task-id').value = task.id;
    document.getElementById('task-title').value = task.title;
    document.getElementById('task-assigned').value = task.assignedToUserId;
    document.getElementById('task-priority').value = task.priority;
    document.getElementById('task-due-date').value = task.dueDate ? task.dueDate.split('T')[0] : '';
    document.getElementById('task-description').value = task.description;
    document.getElementById('task-status').value = normalizeTaskStatus(task.status);
    document.getElementById('task-status-field').style.display = 'flex';
}

async function handleTaskSubmit(event) {
    event.preventDefault();

    const taskId = document.getElementById('task-id').value;
    const assignedToUserId = document.getElementById('task-assigned').value || users[0]?.id || window.currentUser?.storedUser?.id || window.currentUser?.raw?.nameid || '';
    if (!assignedToUserId) {
        showToast(window.t ? window.t('tasks.selectAssignee', 'Please select an assignee before saving') : 'Please select an assignee before saving');
        return;
    }

    const payload = {
        title: document.getElementById('task-title').value.trim(),
        description: document.getElementById('task-description').value.trim(),
        assignedToUserId,
        priority: document.getElementById('task-priority').value,
        dueDate: document.getElementById('task-due-date').value ? new Date(document.getElementById('task-due-date').value).toISOString() : null,
        status: document.getElementById('task-status').value || 'New'
    };

    const response = await fetch(taskId ? `/api/tasks/${taskId}` : '/api/tasks', {
        method: taskId ? 'PUT' : 'POST',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
        },
        body: JSON.stringify(payload)
    });

    if (response.ok) {
        closeTaskModal();
        showToast(taskId ? (window.t ? window.t('tasks.updated', 'Task updated') : 'Task updated') : (window.t ? window.t('tasks.created', 'Task created') : 'Task created'));
        loadTasks();
        window.dispatchEvent(new CustomEvent('qore-notifications-refresh'));
    }
}

async function updateTaskStatus(taskId, status) {
    const response = await fetch(`/api/tasks/${taskId}/status`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
        },
        body: JSON.stringify({ status })
    });

    if (response.ok) {
        showToast(window.t ? window.t('tasks.statusUpdated', 'Status updated') : 'Status updated');
        loadTasks();
        window.dispatchEvent(new CustomEvent('qore-notifications-refresh'));
    }
}

async function deleteTask(taskId) {
    const task = tasks.find((item) => item.id === taskId);
    if (!task || !window.confirm(`Delete ${task.title}?`)) {
        return;
    }

    const response = await fetch(`/api/tasks/${taskId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}` }
    });

    if (response.ok) {
        showToast(window.t ? window.t('tasks.deleted', 'Task deleted') : 'Task deleted');
        loadTasks();
        window.dispatchEvent(new CustomEvent('qore-notifications-refresh'));
    }
}

function bindTasksSection() {
    if (!tasksSection) {
        return;
    }

    const createButton = tasksSection.querySelector('.add-task-button');
    if (createButton) {
        createButton.removeAttribute('hidden');
        createButton.addEventListener('click', () => {
            openTaskModal();
            const title = document.getElementById('task-modal-title');
            if (title) {
                title.textContent = window.t ? window.t('tasks.modalTitleCreate', 'Create Task') : 'Create Task';
            }
            const taskIdInput = document.getElementById('task-id');
            if (taskIdInput) {
                taskIdInput.value = '';
            }
            const statusField = document.getElementById('task-status-field');
            if (statusField) {
                statusField.style.display = 'none';
            }
        });
    }

    const filter = tasksSection.querySelector('.tasks-assignee-filter');
    if (filter) {
        filter.removeAttribute('hidden');
        filter.addEventListener('change', (event) => {
            assigneeFilter = event.target.value;
            renderTasks();
        });
    }
}

function initializeTasksSection() {
    bindTasksSection();
    loadTasks();
}

if (document.readyState === 'loading') {
    window.addEventListener('DOMContentLoaded', initializeTasksSection);
} else {
    initializeTasksSection();
}

window.addEventListener('qore-language-changed', () => {
    renderTasks();
    populateAssigneeFilter();
});
})();
