(function() {
const form = document.getElementById('customer-form');
const list = document.getElementById('customer-list');
const resetButton = document.getElementById('reset-button');
const resetMessage = document.getElementById('reset-message');

const t = (key, fallback) => window.t ? window.t(key, fallback) : fallback;

async function loadCustomers() {
    try {
        const response = await fetch('/api/customers');
        if (!response.ok) {
            list.innerHTML = `<li class="empty-state">${t('customers.unable', 'Unable to load customers right now.')}</li>`;
            return false;
        }

        const customers = await response.json();
        list.innerHTML = customers.map(customer => `<li>${customer.name} — ${customer.company || 'No company'}</li>`).join('');
        return true;
    } catch (error) {
        list.innerHTML = `<li class="empty-state">${t('customers.unable', 'Unable to load customers right now.')}</li>`;
        return false;
    }
}

form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const payload = {
        name: document.getElementById('name').value,
        email: document.getElementById('email').value,
        phone: document.getElementById('phone').value,
        company: document.getElementById('company').value,
        status: 'Active'
    };

    const response = await fetch('/api/customers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        list.innerHTML = `<li class="empty-state">${t('customers.saveFailed', 'Unable to save customer right now.')}</li>`;
        return;
    }

    form.reset();
    await loadCustomers();
});

resetButton.addEventListener('click', async () => {
    const confirmed = window.confirm(t('auth.resetConfirmText', 'Reset Qore to the initial state? This removes CRM data and restores the configured administrator account and default theme.'));
    if (!confirmed) {
        return;
    }

    resetMessage.textContent = t('auth.resetting', 'Resetting...');

    try {
        const response = await fetch('/api/system/reset', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${sessionStorage.getItem('qore_token')}`
            },
            body: JSON.stringify({ confirm: true })
        });

        const result = await response.json();

        if (!response.ok) {
            resetMessage.textContent = result.message || t('auth.resetFailed', 'Reset failed.');
            return;
        }

        resetMessage.textContent = result.message || t('auth.resetComplete', 'Reset complete.');
        await loadCustomers();
    } catch (error) {
        resetMessage.textContent = t('auth.resetFailed', 'Reset failed.');
    }
});

loadCustomers();
})();
