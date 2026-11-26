(function() {
const staffSection = document.querySelector('[data-section-content="staff"]');

const t = (key, fallback) => window.t ? window.t(key, fallback) : fallback;

function createBadge(text, variant = '') {
    const badge = document.createElement('span');
    badge.className = `badge ${variant}`.trim();
    badge.textContent = text;
    return badge;
}

function getStaffPanel() {
    if (!staffSection) {
        return null;
    }

    return staffSection.querySelector(':scope > .section-card') || staffSection.querySelector('.section-card');
}

function renderStaffTable(users, isAdmin) {
    const panel = getStaffPanel() || document.createElement('div');
    if (!panel.parentElement) {
        panel.className = 'section-card';
        staffSection?.appendChild(panel);
    }

    panel.className = 'section-card staff-panel';
    panel.innerHTML = '';

    const toolbar = document.createElement('div');
    toolbar.className = 'staff-toolbar';

    const searchInput = document.createElement('input');
    searchInput.className = 'staff-search';
    searchInput.type = 'search';
    searchInput.placeholder = t('staff.searchPlaceholder', 'Search staff by name or role');

    toolbar.appendChild(searchInput);
    panel.appendChild(toolbar);

    const tableWrapper = document.createElement('div');
    tableWrapper.className = 'staff-table-wrapper';

    const table = document.createElement('table');
    table.className = 'staff-table';
    table.innerHTML = `
        <thead>
            <tr>
                <th>${t('staff.fullName', 'Full Name')}</th>
                <th>${t('staff.username', 'Username')}</th>
                <th>${t('staff.role', 'Role')}</th>
                <th>${t('staff.email', 'Email')}</th>
                <th>${t('staff.status', 'Status')}</th>
            </tr>
        </thead>
        <tbody></tbody>
    `;

    const tbody = table.querySelector('tbody');

    const applyFilter = () => {
        const term = searchInput.value.toLowerCase();
        const rows = tbody.querySelectorAll('tr');
        rows.forEach((row) => {
            const haystack = (row.dataset.search || '').toLowerCase();
            row.style.display = haystack.includes(term) ? '' : 'none';
        });
    };

    searchInput.addEventListener('input', applyFilter);

    users.forEach((user) => {
        const row = document.createElement('tr');
        row.className = 'staff-row';
        row.dataset.search = `${user.fullName} ${user.role} ${user.username}`.toLowerCase();
        row.innerHTML = `
            <td>${user.fullName}</td>
            <td>${user.username}</td>
            <td>${user.role}</td>
            <td>${user.email}</td>
            <td>${user.isActive ? t('staff.active', 'Active') : t('staff.inactive', 'Inactive')}</td>
        `;

        tbody.appendChild(row);
    });

    tableWrapper.appendChild(table);
    panel.appendChild(tableWrapper);
}

async function loadStaff() {
    if (!staffSection) {
        return;
    }

    let panel = getStaffPanel();
    if (!panel) {
        panel = document.createElement('div');
        panel.className = 'section-card';
        staffSection.appendChild(panel);
    } else {
        panel.className = 'section-card';
    }

    panel.innerHTML = `<h4>${t('section.staff', 'Staff')}</h4><p>${t('staff.loading', 'Loading staff directory…')}</p>`;

    const token = sessionStorage.getItem('qore_token');
    const userRole = window.currentUser?.role || 'Employee';
    const isAdmin = userRole === 'Admin';

    try {
        let users = [];

        if (isAdmin) {
            const response = await fetch('/api/admin/users', {
                headers: { Authorization: `Bearer ${token || ''}` }
            });
            if (!response.ok) {
                throw new Error('Unable to fetch staff');
            }
            users = await response.json();
        } else {
            users = [
                {
                    id: 'self',
                    fullName: window.currentUser?.storedUser?.fullName || window.currentUser?.fullName || 'Current User',
                    username: window.currentUser?.username || 'user',
                    role: userRole,
                    email: window.currentUser?.storedUser?.email || 'n/a',
                    isActive: true
                }
            ];
        }

        renderStaffTable(users, isAdmin);
    } catch (error) {
        if (panel) {
            panel.className = 'section-card';
            panel.innerHTML = `<h4>${t('section.staff', 'Staff')}</h4><p>${t('staff.unable', 'Unable to load staff data.')}</p>`;
        }
    }
}

window.addEventListener('DOMContentLoaded', loadStaff);
window.addEventListener('qore-language-changed', () => {
    loadStaff();
});
})();
