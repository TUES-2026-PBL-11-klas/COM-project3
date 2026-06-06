const API_BASE_DEFAULT = 'https://com-project3.onrender.com';
const IS_LOCAL = location.hostname === 'localhost' || location.hostname === '127.0.0.1';

const API_CANDIDATES = [
  ...(IS_LOCAL ? [
    'http://localhost:5432',
    'http://localhost:30543',
    'http://localhost:5210',
    'http://localhost:7210',
    'http://localhost:5000',
    'http://localhost:5001'
  ] : []),
  API_BASE_DEFAULT
];

let API_BASE = localStorage.getItem('cm_api_base') || '';
if (!IS_LOCAL && (API_BASE.includes('localhost') || API_BASE.includes('127.0.0.1'))) {
  API_BASE = '';
}

const elements = {
  tabLogin: document.getElementById('tabLogin'),
  tabSignup: document.getElementById('tabSignup'),
  loginForm: document.getElementById('loginForm'),
  signupForm: document.getElementById('signupForm'),
  loginEmail: document.getElementById('loginEmail'),
  loginPassword: document.getElementById('loginPassword'),
  signupUsername: document.getElementById('signupUsername'),
  signupEmail: document.getElementById('signupEmail'),
  signupPassword: document.getElementById('signupPassword'),
  submitLogin: document.getElementById('submitLogin'),
  submitSignup: document.getElementById('submitSignup'),
  loginError: document.getElementById('loginError'),
  signupError: document.getElementById('signupError'),
  toast: document.getElementById('toast')
};

const nextPage = (() => {
  const urlParams = new URLSearchParams(window.location.search);
  let next = urlParams.get('next');
  if (next) {
    try {
      next = decodeURIComponent(next);
    } catch {
      // ignore invalid encoding
    }
  }

  if (!next && urlParams.has('id')) {
    next = `team-detail.html?id=${encodeURIComponent(urlParams.get('id') || '')}`;
  }

  return next || 'index.html';
})();

async function detectApiBase() {
  const candidates = API_CANDIDATES.slice();
  if (API_BASE && !candidates.includes(API_BASE)) candidates.unshift(API_BASE);

  for (const base of candidates) {
    try {
      const res = await fetch(`${base}/api/cleanmap/ping`, { signal: AbortSignal.timeout(6000) });
      if (res.ok) {
        API_BASE = base;
        localStorage.setItem('cm_api_base', base);
        return true;
      }
    } catch {
      // ignore
    }
  }

  localStorage.removeItem('cm_api_base');
  return false;
}

function showToast(message, isError = false) {
  elements.toast.textContent = message;
  elements.toast.className = 'toast show' + (isError ? ' error' : '');
  clearTimeout(elements.toast._timer);
  elements.toast._timer = setTimeout(() => elements.toast.classList.remove('show'), 3000);
}

function switchTab(mode) {
  if (mode === 'login') {
    elements.tabLogin.classList.add('active');
    elements.tabSignup.classList.remove('active');
    elements.loginForm.classList.remove('hidden');
    elements.signupForm.classList.add('hidden');
  } else {
    elements.tabSignup.classList.add('active');
    elements.tabLogin.classList.remove('active');
    elements.signupForm.classList.remove('hidden');
    elements.loginForm.classList.add('hidden');
  }
}

async function fetchJson(url, options = {}, timeoutMs = 8000) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(url, { ...options, signal: controller.signal });
    const body = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(body?.error || response.statusText || `HTTP ${response.status}`);
    }
    return body;
  } finally {
    clearTimeout(timeout);
  }
}

async function login() {
  elements.submitLogin.disabled = true;
  elements.loginError.textContent = '';

  try {
    await ensureApi();
    const body = {
      email: elements.loginEmail.value.trim(),
      password: elements.loginPassword.value
    };
    const data = await fetchJson(`${API_BASE}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    saveSession(data);
    showToast('Login successful. Redirecting...');
    setTimeout(() => window.location.href = nextPage, 700);
  } catch (err) {
    elements.loginError.textContent = err.message || 'Login failed.';
  } finally {
    elements.submitLogin.disabled = false;
  }
}

async function signup() {
  elements.submitSignup.disabled = true;
  elements.signupError.textContent = '';

  try {
    await ensureApi();
    const body = {
      username: elements.signupUsername.value.trim(),
      email: elements.signupEmail.value.trim(),
      password: elements.signupPassword.value
    };
    const data = await fetchJson(`${API_BASE}/api/auth/signup`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    saveSession(data);
    showToast('Account created. Redirecting...');
    setTimeout(() => window.location.href = nextPage, 700);
  } catch (err) {
    elements.signupError.textContent = err.message || 'Signup failed.';
  } finally {
    elements.submitSignup.disabled = false;
  }
}

function saveSession(data) {
  localStorage.setItem('cm_access_token', data.accessToken);
  localStorage.setItem('cm_user', JSON.stringify(data.user));
  localStorage.setItem('cm_user_id', data.user?.id || data.user?.userId || '');
}

async function ensureApi() {
  const connected = await detectApiBase();
  if (!connected) throw new Error('Could not connect to the API.');
}

function init() {
  elements.tabLogin.addEventListener('click', () => switchTab('login'));
  elements.tabSignup.addEventListener('click', () => switchTab('signup'));
  elements.submitLogin.addEventListener('click', () => login());
  elements.submitSignup.addEventListener('click', () => signup());
}

init();
