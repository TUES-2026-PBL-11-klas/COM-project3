const API_CANDIDATES = [
  'https://com-project3.onrender.com',
  'http://localhost:5432',
  'http://localhost:30543',
  'http://localhost:5210',
  'http://localhost:7210',
  'http://localhost:5000',
  'http://localhost:5001',
];

let API_BASE = localStorage.getItem('cm_api_base') || '';
let currentUserId = localStorage.getItem('cm_user_id') || '';

document.getElementById('userIdInput').value = currentUserId;
updateUserLabel();

if (!currentUserId) {
  showToast('Set your user ID above to get started.', false);
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
    document.getElementById('teamsContainer').innerHTML =
      `<div class="empty-state"><p>Could not connect to API. Is the server running?</p></div>`;
    return;
  }
  if (currentUserId) loadTeams();
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
  el.textContent = currentUserId ? `Signed in as: ${currentUserId}` : '';
}

async function loadTeams() {
  if (!currentUserId) return;
  document.getElementById('teamsContainer').innerHTML =
    '<div class="teams-loading"><div class="spinner"></div></div>';

  try {
    const res = await fetch(`${API_BASE}/api/teams/my?userId=${encodeURIComponent(currentUserId)}`);
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
    const res = await fetch(`${API_BASE}/api/teams?userId=${encodeURIComponent(currentUserId)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
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
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}?userId=${encodeURIComponent(currentUserId)}`,
      { method: 'DELETE' }
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
