function getJwtPayload(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map((char) => `%${(`00${char.charCodeAt(0).toString(16)}`).slice(-2)}`).join(''));
        return JSON.parse(jsonPayload);
    } catch (error) {
        return null;
    }
}

function getCurrentUserFromStorage() {
    const token = sessionStorage.getItem('qore_token');
    if (!token) {
        return null;
    }

    const payload = getJwtPayload(token);
    if (!payload) {
        return null;
    }

    const now = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp <= now) {
        sessionStorage.removeItem('qore_token');
        sessionStorage.removeItem('qore_user');
        return null;
    }

    const storedUser = sessionStorage.getItem('qore_user');
    return {
        token,
        role: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'User',
        username: payload.username || payload.name || 'User',
        fullName: payload.fullName || payload.name || 'User',
        raw: payload,
        storedUser: storedUser ? JSON.parse(storedUser) : null
    };
}

function protectAuthenticatedPage() {
    const user = getCurrentUserFromStorage();
    if (!user) {
        window.location.href = '/login.html';
        return null;
    }

    window.currentUser = user;
    return user;
}

window.protectAuthenticatedPage = protectAuthenticatedPage;
window.getCurrentUserFromStorage = getCurrentUserFromStorage;
