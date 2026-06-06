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

let API_BASE = localStorage.getItem('cm_api_base') || API_BASE_DEFAULT;
if (!IS_LOCAL && (API_BASE.includes('localhost') || API_BASE.includes('127.0.0.1'))) {
  API_BASE = API_BASE_DEFAULT;
  localStorage.setItem('cm_api_base', API_BASE);
}
let currentUserId = localStorage.getItem('cm_user_id') || '';
let currentUserName = '';

updateUserLabel();
attachAuthButton();

if (!currentUserId) {
  showToast('Tap Login to sign in or register.', false);
}

detectApiAndLoad();

async function detectApiAndLoad() {
  const candidates = API_CANDIDATES.slice();
  if (API_BASE && !candidates.includes(API_BASE)) candidates.unshift(API_BASE);

  let found = '';
  for (const base of candidates) {
    try {
      const res = await fetch(`${base}/api/cleanmap/ping`, { signal: AbortSignal.timeout(6000) });
      if (res.ok) { found = base; break; }
    } catch {  }
  }

  if (found) {
    API_BASE = found;
    localStorage.setItem('cm_api_base', found);
  }

  if (!API_BASE) {
    localStorage.removeItem('cm_api_base');
    document.getElementById('teamsContainer').innerHTML =
      `<div class="empty-state"><p>Could not connect to API. Is the server running?</p></div>`;
    return;
  }

  await restoreUserState();

  if (currentUserId) {
    loadTeams();
  } else {
    window.location.href = 'login.html?next=teams.html';
  }
}

async function restoreUserState() {
  const savedUser = localStorage.getItem('cm_user');
  const savedToken = localStorage.getItem('cm_access_token');

  if (savedUser) {
    try {
      const parsed = JSON.parse(savedUser);
      if (parsed?.id) {
        currentUserId = parsed.id;
        currentUserName = parsed?.username || '';
        localStorage.setItem('cm_user_id', currentUserId);
      }
    } catch {
      // ignore invalid saved user
    }
  }

  if (!currentUserId && savedToken) {
    try {
      const res = await fetch(`${API_BASE}/api/auth/me`, {
        headers: { Authorization: `Bearer ${savedToken}` },
        signal: AbortSignal.timeout(6000)
      });
      if (res.ok) {
        const user = await res.json();
        if (user?.id) {
          currentUserId = user.id;
          currentUserName = user?.username || '';
          localStorage.setItem('cm_user_id', currentUserId);
          localStorage.setItem('cm_user', JSON.stringify(user));
        }
      }
    } catch {
      // ignore restore failures
    }
  }

  updateUserLabel();
}

function setUserId() {
  const val = document.getElementById('userIdInput').value.trim();
  if (!val) { showToast('Enter a user ID.', true); return; }
  currentUserId = val;
  localStorage.setItem('cm_user_id', val);
  updateUserLabel();
  loadTeams();
}

function updateUserLabel() {
  const el = document.getElementById('currentUserLabel');
  const userDisplay = currentUserName || currentUserId;
  el.textContent = userDisplay ? `Signed in as: ${userDisplay}` : '';
  const authBtn = document.getElementById('authBtn');
  if (authBtn) {
    authBtn.textContent = currentUserId ? 'Logout' : 'Login';
  }
}

function getAuthHeaders() {
  const token = localStorage.getItem('cm_access_token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function attachAuthButton() {
  const authBtn = document.getElementById('authBtn');
  if (!authBtn) return;

  authBtn.addEventListener('click', () => {
    if (currentUserId) {
      localStorage.removeItem('cm_user_id');
      localStorage.removeItem('cm_access_token');
      localStorage.removeItem('cm_user');
      currentUserId = '';
      currentUserName = '';
      updateUserLabel();
      showToast('Logged out.');
      document.getElementById('teamsContainer').innerHTML =
        `<div class="empty-state"><p>Login to see your teams.</p></div>`;
    } else {
      window.location.href = `login.html?next=${encodeURIComponent('teams.html')}`;
    }
  });
}

async function loadTeams() {
  if (!currentUserId) return;
  document.getElementById('teamsContainer').innerHTML =
    '<div class="teams-loading"><div class="spinner"></div></div>';

  try {
    const res = await fetch(`${API_BASE}/api/teams/my`, {
      headers: getAuthHeaders()
    });
    const teams = await res.json();
    renderTeams(teams);
  } catch {
    document.getElementById('teamsContainer').innerHTML =
      `<div class="empty-state"><p>Could not load teams.</p></div>`;
  }
}

function renderTeams(teams) {
  const container = document.getElementById('teamsContainer');

  if (!teams.length) {
    container.innerHTML = `
      <div class="empty-state">
        <svg width="48" height="48" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
          <circle cx="9" cy="7" r="4"/>
          <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
          <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
        </svg>
        <p>You are not part of any team yet.</p>
      </div>`;
    return;
  }

  container.innerHTML = `<div class="teams-grid">${teams.map(teamCard).join('')}</div>`;
}

function teamCard(team) {
  const isOwner = team.createdBy === currentUserId;
  const role = isOwner ? 'owner' : 'member';
  const count = team.members.length;

  return `
    <div class="team-card" onclick="goToTeam('${esc(team.id)}')">
      <h3>${esc(team.name)}</h3>
      <p>${esc(team.description ?? 'No description.')}</p>
      <div class="team-card-footer">
        <span class="member-count">${count} member${count !== 1 ? 's' : ''}</span>
        <span class="role-badge role-${role}">${role}</span>
        ${isOwner ? `<button class="btn-delete-team" onclick="event.stopPropagation(); deleteTeam('${esc(team.id)}', '${esc(team.name)}')">&times; Delete</button>` : ''}
      </div>
    </div>`;
}

function goToTeam(id) {
  window.location.href = `team-detail.html?id=${encodeURIComponent(id)}`;
}

function openCreateModal() {
  if (!currentUserId) { showToast('Set your user ID first.', true); return; }
  document.getElementById('createModal').classList.add('open');
  document.getElementById('teamName').focus();
}

function closeCreateModal() {
  document.getElementById('createModal').classList.remove('open');
  document.getElementById('teamName').value = '';
  document.getElementById('teamDesc').value = '';
}

function handleOverlayClick(e) {
  if (e.target === e.currentTarget) closeCreateModal();
}

async function createTeam() {
  const name = document.getElementById('teamName').value.trim();
  const description = document.getElementById('teamDesc').value.trim();

  if (!name) { showToast('Team name is required.', true); return; }

  try {
    const res = await fetch(`${API_BASE}/api/teams`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...getAuthHeaders() },
      body: JSON.stringify({ name, description }),
    });

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Failed to create team.', true);
      return;
    }

    closeCreateModal();
    showToast('Team created!');
    loadTeams();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

async function deleteTeam(teamId, teamName) {
  if (!confirm(`Delete team "${teamName}"? This cannot be undone.`)) return;

  try {
    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}`,
      { method: 'DELETE', headers: getAuthHeaders() }
    );

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Failed to delete team.', true);
      return;
    }

    showToast('Team deleted.');
    loadTeams();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

function showToast(message, isError = false) {
  const toast = document.getElementById('toast');
  toast.textContent = message;
  toast.className = 'toast show' + (isError ? ' error' : '');
  clearTimeout(toast._timer);
  toast._timer = setTimeout(() => toast.classList.remove('show'), 3000);
}

function esc(str) {
  const d = document.createElement('div');
  d.textContent = String(str);
  return d.innerHTML;
}
