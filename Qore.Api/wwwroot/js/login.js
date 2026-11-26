const statusMessage = document.getElementById('status-message');
const loginForm = document.getElementById('login-form');

function setStatus(message, type = '') {
    statusMessage.textContent = message || '';
    statusMessage.className = `status-message${type ? ` ${type}` : ''}`;
}

loginForm.addEventListener('submit', async (event) => {
    event.preventDefault();

    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;

    if (!username || !password) {
        setStatus(window.t ? window.t('auth.usernamePasswordRequired', 'Username and password are required.') : 'Username and password are required.', 'error');
        return;
    }

    setStatus(window.t ? window.t('auth.signingIn', 'Signing you in...') : 'Signing you in...');

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const result = await response.json();

        if (response.ok && result.success) {
            sessionStorage.setItem('qore_token', result.token);
            sessionStorage.setItem('qore_user', JSON.stringify(result.user));
            window.location.href = '/dashboard.html';
            return;
        }

        setStatus(result.message || (window.t ? window.t('auth.loginFailed', 'Login failed. Please try again.') : 'Login failed. Please try again.'), 'error');
    } catch (error) {
        setStatus(window.t ? window.t('auth.signInFailed', 'Unable to sign in right now. Please try again.') : 'Unable to sign in right now. Please try again.', 'error');
    }
});
