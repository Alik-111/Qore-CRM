const dashboardShell = document.getElementById('dashboard-widget-shell');

function formatCurrency(value) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value || 0);
}

function formatRelativeDate(value) {
    if (!value) {
        return 'recently updated';
    }

    const date = new Date(value);
    return date.toLocaleString();
}

function animateValue(element, endValue, prefix = '', suffix = '') {
    const duration = 800;
    const startTime = performance.now();
    const startValue = 0;

    const tick = (currentTime) => {
        const progress = Math.min((currentTime - startTime) / duration, 1);
        const eased = 1 - Math.pow(1 - progress, 3);
        const current = Math.round(startValue + (endValue - startValue) * eased);
        element.textContent = `${prefix}${current}${suffix}`;

        if (progress < 1) {
            requestAnimationFrame(tick);
        }
    };

    requestAnimationFrame(tick);
}

function createCard(title, value, detail, extraClass = '', field = '') {
    const card = document.createElement('div');
    card.className = `dashboard-card ${extraClass}`.trim();
    card.innerHTML = `
        <h4>${title}</h4>
        <div class="metric-value" data-animate${field ? ` data-field="${field}"` : ''}>${value}</div>
        <div class="metric-trend">${detail}</div>
    `;
    return card;
}

async function loadDashboard() {
    if (!dashboardShell) {
        console.warn('[Qore] Dashboard shell element not found');
        return;
    }

    dashboardShell.innerHTML = `<div class="dashboard-card"><h4>${window.t ? window.t('dashboard.loadingDashboard', 'Loading dashboard…') : 'Loading dashboard…'}</h4><p>${window.t ? window.t('dashboard.loadingMessage', 'Gathering your latest CRM data.') : 'Gathering your latest CRM data.'}</p></div>`;

    try {
        const response = await fetch('/api/dashboard/summary', {
            headers: {
                Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
            }
        });

        if (!response.ok) {
            throw new Error(`Dashboard API returned ${response.status}`);
        }

        const data = await response.json();
        const totals = data?.totals || {};
        const taskBreakdown = data?.taskBreakdown || {};
        const focusData = data?.focus || {};
        const recentActivity = Array.isArray(data?.recentActivity) ? data.recentActivity : [];
        const myTasksData = Array.isArray(data?.myTasks) ? data.myTasks : [];

        const shell = document.createElement('div');
        shell.className = 'dashboard-shell';

        const topGrid = document.createElement('div');
        topGrid.className = 'dashboard-grid';

        topGrid.appendChild(createCard(window.t ? window.t('dashboard.totalClients', 'Total Clients') : 'Total Clients', '0', window.t ? window.t('dashboard.totalClientsDetail', 'Live client count') : 'Live client count', '', 'clients'));
        topGrid.appendChild(createCard(window.t ? window.t('dashboard.openOpportunities', 'Open Opportunities') : 'Open Opportunities', '0', window.t ? window.t('dashboard.openOpportunitiesDetail', 'Pipeline still active') : 'Pipeline still active', '', 'openOpportunities'));
        topGrid.appendChild(createCard(window.t ? window.t('dashboard.wonPipelineValue', 'Won Pipeline Value') : 'Won Pipeline Value', '$0', window.t ? window.t('dashboard.wonPipelineValueDetail', 'Closed deals value') : 'Closed deals value', '', 'wonPipelineValue'));
        topGrid.appendChild(createCard(window.t ? window.t('dashboard.documents', 'Documents') : 'Documents', '0', window.t ? window.t('dashboard.documentsDetail', 'Accessible records') : 'Accessible records', '', 'documents'));

        const focusCard = document.createElement('div');
        focusCard.className = 'dashboard-card';
        focusCard.innerHTML = `
            <h4>${window.t ? window.t('dashboard.priorityFocus', 'Priority Focus') : 'Priority Focus'}</h4>
            <div class="metric-value" data-animate>0</div>
            <div class="metric-trend">${window.t ? window.t('dashboard.priorityFocusDetail', 'Overdue tasks and upcoming deadlines') : 'Overdue tasks and upcoming deadlines'}</div>
        `;
        topGrid.appendChild(focusCard);

        shell.appendChild(topGrid);

        const bottomGrid = document.createElement('div');
        bottomGrid.className = 'dashboard-grid';

        const taskStatuses = [
            { key: 'new', label: 'New', value: taskBreakdown.New || 0 },
            { key: 'inprogress', label: 'In Progress', value: taskBreakdown['In Progress'] || 0 },
            { key: 'completed', label: 'Completed', value: taskBreakdown.Completed || 0 }
        ];
        const totalTasks = Math.max(1, taskStatuses.reduce((sum, item) => sum + item.value, 0));
        const taskStatusLabel = window.t ? window.t('dashboard.taskStatus', 'Task Status') : 'Task Status';
        const taskCountLabel = window.t ? window.t('dashboard.totalTasks', 'total') : 'total';

        const tasksCard = document.createElement('div');
        tasksCard.className = 'dashboard-card';
        tasksCard.innerHTML = `
            <h4 class="dashboard-card-title">
                <span>${taskStatusLabel}</span>
                <span class="badge">${totalTasks} ${taskCountLabel}</span>
            </h4>
            <div class="progress-group">
                <div class="progress-row" data-status="new">
                    <div class="progress-row-header"><span>${window.t ? window.t('dashboard.new', 'New') : 'New'}</span><strong data-count="new">0</strong></div>
                    <div class="progress-bar"><span data-progress="new" style="width:0%"></span></div>
                </div>
                <div class="progress-row" data-status="inprogress">
                    <div class="progress-row-header"><span>${window.t ? window.t('dashboard.inProgress', 'In Progress') : 'In Progress'}</span><strong data-count="inprogress">0</strong></div>
                    <div class="progress-bar"><span data-progress="inprogress" style="width:0%"></span></div>
                </div>
                <div class="progress-row" data-status="completed">
                    <div class="progress-row-header"><span>${window.t ? window.t('dashboard.completed', 'Completed') : 'Completed'}</span><strong data-count="completed">0</strong></div>
                    <div class="progress-bar"><span data-progress="completed" style="width:0%"></span></div>
                </div>
            </div>
        `;

        const activityCard = document.createElement('div');
        activityCard.className = 'dashboard-card';
        activityCard.innerHTML = `<h4>${window.t ? window.t('dashboard.recentActivity', 'Recent Activity') : 'Recent Activity'}</h4><ul class="activity-list"></ul>`;

        const myTasksCard = document.createElement('div');
        myTasksCard.className = 'dashboard-card';
        myTasksCard.innerHTML = `<h4>${window.t ? window.t('dashboard.myTasks', 'My Tasks') : 'My Tasks'}</h4><ul class="task-list"></ul>`;

        bottomGrid.appendChild(tasksCard);
        bottomGrid.appendChild(activityCard);
        bottomGrid.appendChild(myTasksCard);

        shell.appendChild(bottomGrid);
        dashboardShell.innerHTML = '';
        dashboardShell.appendChild(shell);

        const metricValues = dashboardShell.querySelectorAll('[data-animate]');

        metricValues.forEach((valueElement) => {
            const field = valueElement.getAttribute('data-field');
            let value = 0;
            let prefix = '';

            switch (field) {
                case 'clients':
                    value = Number(totals.clients) || 0;
                    break;
                case 'openOpportunities':
                    value = Number(totals.openOpportunities) || 0;
                    break;
                case 'wonPipelineValue':
                    value = Number(totals.wonPipelineValue) || 0;
                    prefix = '$';
                    break;
                case 'documents':
                    value = Number(totals.documents) || 0;
                    break;
                default:
                    value = Number(valueElement.textContent.replace(/[^0-9.-]+/g, '')) || 0;
            }

            animateValue(valueElement, value, prefix, '');
        });

        const focusMessage = focusCard.querySelector('.metric-trend');
        if (focusMessage) {
            focusMessage.textContent = `${focusData.overdueTasks || 0} overdue • ${focusData.upcomingTasks || 0} due soon`;
        }

        taskStatuses.forEach((item) => {
            const countElement = dashboardShell.querySelector(`[data-count="${item.key}"]`);
            const barElement = dashboardShell.querySelector(`[data-progress="${item.key}"]`);
            if (countElement) {
                countElement.textContent = item.value;
            }
            if (barElement) {
                barElement.style.width = `${Math.round((item.value / totalTasks) * 100)}%`;
            }
        });

        const activityList = dashboardShell.querySelector('.activity-list');
        const activityItems = recentActivity.slice(0, 6);
        if (activityItems.length) {
            activityList.innerHTML = activityItems.map((item) => `
                <li class="activity-item">
                    <div>
                        <strong>${item.title}</strong>
                        <div class="small-muted">${item.type} • ${item.status || 'Updated'}</div>
                    </div>
                    <span class="badge">${formatRelativeDate(item.createdAt)}</span>
                </li>
            `).join('');
        } else {
            activityList.innerHTML = `<li class="activity-item"><div><strong>${window.t ? window.t('dashboard.noActivity', 'No recent activity') : 'No recent activity'}</strong><div class="small-muted">${window.t ? window.t('dashboard.noActivityDetail', 'Create a client, task, or opportunity to see updates.') : 'Create a client, task, or opportunity to see updates.'}</div></div></li>`;
        }

        const taskList = dashboardShell.querySelector('.task-list');
        const myTasks = (data.myTasks || []).slice(0, 5);
        if (myTasks.length) {
            taskList.innerHTML = myTasks.map((task) => `
                <li class="task-item">
                    <div>
                        <strong>${task.title}</strong>
                        <div class="small-muted">${task.priority} • ${task.status}</div>
                    </div>
                    <span class="badge">${task.dueDate ? new Date(task.dueDate).toLocaleDateString() : (window.t ? window.t('dashboard.noDueDate', 'No due date') : 'No due date')}</span>
                </li>
            `).join('');
        } else {
            taskList.innerHTML = `<li class="task-item"><div><strong>${window.t ? window.t('dashboard.noPendingTasks', 'No pending tasks') : 'No pending tasks'}</strong><div class="small-muted">${window.t ? window.t('dashboard.noPendingTasksDetail', 'You are all caught up.') : 'You are all caught up.'}</div></div></li>`;
        }
    } catch (error) {
        console.error('[Qore] Dashboard loading error:', error);
        if (dashboardShell) {
            dashboardShell.innerHTML = `<div class="dashboard-card"><h4>${window.t ? window.t('dashboard.dashboardUnavailable', 'Dashboard unavailable') : 'Dashboard unavailable'}</h4><p>${window.t ? window.t('dashboard.dashboardUnavailableDetail', 'Unable to load the latest CRM data right now. Please refresh the page.') : 'Unable to load the latest CRM data right now. Please refresh the page.'}</p></div>`;
        }
    }
}

async function loadNotifications() {
    const list = document.querySelector('.notifications-list');
    if (!list) {
        return;
    }

    list.innerHTML = `<div class="notification-empty">${window.t ? window.t('dashboard.loadingNotifications', 'Loading notifications…') : 'Loading notifications…'}</div>`;

    try {
        const response = await fetch('/api/dashboard/notifications', {
            headers: {
                Authorization: `Bearer ${sessionStorage.getItem('qore_token') || ''}`
            }
        });

        if (!response.ok) {
            throw new Error(`Notifications API returned ${response.status}`);
        }

        const data = await response.json();
        const notifications = data.notifications || [];

        if (!notifications.length) {
            list.innerHTML = `<div class="notification-empty">${window.t ? window.t('dashboard.noNotifications', 'No notifications yet. You will see task and pipeline reminders here.') : 'No notifications yet. You will see task and pipeline reminders here.'}</div>`;
            return;
        }

        list.innerHTML = notifications.map((item) => `
            <div class="notification-item">
                <div>
                    <div class="notification-title">
                        <strong>${item.title}</strong>
                        <span class="notification-badge ${item.priority || 'low'}">${item.priority || 'info'}</span>
                    </div>
                    <div class="small-muted">${item.message}</div>
                </div>
                <span class="badge">${formatRelativeDate(item.createdAt)}</span>
            </div>
        `).join('');
    } catch (error) {
        console.error('[Qore] Notifications loading error:', error);
        list.innerHTML = `<div class="notification-empty">${window.t ? window.t('dashboard.notificationsUnavailable', 'Unable to load notifications right now.') : 'Unable to load notifications right now.'}</div>`;
    }
}

window.addEventListener('DOMContentLoaded', () => {
    try {
        loadDashboard();
        loadNotifications();
    } catch (error) {
        console.error('[Qore] Dashboard initialization failed:', error);
    }
});

window.addEventListener('qore-language-changed', () => {
    loadDashboard();
    loadNotifications();
});

window.addEventListener('qore-notifications-refresh', () => {
    loadNotifications();
});
