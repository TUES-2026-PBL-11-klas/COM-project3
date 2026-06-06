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

const params = new URLSearchParams(window.location.search);
const teamId = params.get('id');
if (!teamId) window.location.href = 'teams.html';

let API_BASE = localStorage.getItem('cm_api_base') || '';
if (!IS_LOCAL && (API_BASE.includes('localhost') || API_BASE.includes('127.0.0.1'))) {
  API_BASE = '';
}
let currentUserId = localStorage.getItem('cm_user_id') || '';
let currentUserName = '';
let currentTeam = null;

attachAuthButton();
updateUserLabel();

detectApiAndLoad();

async function detectApiAndLoad() {
  const candidates = API_CANDIDATES.slice();
  if (API_BASE && !candidates.includes(API_BASE)) candidates.unshift(API_BASE);

  let found = '';
  for (const base of candidates) {
    try {
      const res = await fetch(`${base}/api/cleanmap/ping`, { signal: AbortSignal.timeout(6000) });
      if (res.ok) { found = base; break; }
    } catch { /* try next */ }
  }

  if (found) {
    API_BASE = found;
    localStorage.setItem('cm_api_base', found);
  }

  if (!API_BASE) {
    showToast('Could not connect to API.', true);
    return;
  }

  await restoreUserState();
  loadTeam();
}

async function restoreUserState() {
  const savedUser = localStorage.getItem('cm_user');
  const savedToken = localStorage.getItem('cm_access_token');

  if (savedUser) {
    try {
      const parsed = JSON.parse(savedUser);
      currentUserId = parsed?.id || currentUserId;
      currentUserName = parsed?.username || currentUserName;
    } catch {
      currentUserName = '';
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
        currentUserId = user?.id || currentUserId;
        currentUserName = user?.username || currentUserName;
        localStorage.setItem('cm_user', JSON.stringify(user));
        localStorage.setItem('cm_user_id', currentUserId);
      }
    } catch {
      // ignore restore failures
    }
  }

  updateUserLabel();
}

function updateUserLabel() {
  const navLabel = document.getElementById('navUserLabel');
  if (navLabel) {
    navLabel.textContent = currentUserId ? `You: ${currentUserName || currentUserId}` : '';
  }
}

// ---- Load team ----
async function loadTeam() {
  document.getElementById('pageLoader').style.display = 'flex';
  try {
    const res = await fetch(`${API_BASE}/api/teams/${encodeURIComponent(teamId)}`);
    if (!res.ok) { window.location.href = 'teams.html'; return; }

    currentTeam = await res.json();
    renderHeader(currentTeam);
    renderMembers(currentTeam);
    await loadImages();

    document.title = `${currentTeam.name} — Nexora`;
    document.getElementById('navTeamName').textContent = currentTeam.name;
    document.getElementById('pageLoader').style.display = 'none';
    document.getElementById('teamContent').style.display = 'block';
  } catch {
    showToast('Could not load team.', true);
    document.getElementById('pageLoader').style.display = 'none';
  }
}

function renderHeader(team) {
  document.getElementById('teamHeader').innerHTML = `
    <h1>${esc(team.name)}</h1>
    <p class="teams-subtitle">${esc(team.description ?? 'No description.')}</p>`;
}

// ---- Members ----
function renderMembers(team) {
  const isOwner = team.createdBy === currentUserId;
  const list = document.getElementById('membersList');

  list.innerHTML = team.members.map(m => {
    const initials = String(m.userId).substring(0, 2).toUpperCase();
    const joined = new Date(m.joinedAt).toLocaleDateString();
    const canRemove = isOwner && m.userId !== currentUserId;
    const memberName = m.userId === currentUserId
      ? `You${currentUserName ? `: ${currentUserName}` : `: ${m.userId}`}`
      : m.userId;

    return `
      <div class="member-row">
        <div class="member-info">
          <div class="avatar">${initials}</div>
          <div>
            <div class="member-name">${esc(memberName)}</div>
            <div class="member-joined">Joined ${joined}</div>
          </div>
        </div>
        <div style="display:flex;align-items:center;gap:8px">
          <span class="role-badge role-${m.role}">${m.role}</span>
          ${canRemove ? `<button class="btn-danger" onclick="removeMember('${esc(m.userId)}')">Remove</button>` : ''}
        </div>
      </div>`;
  }).join('');

  document.getElementById('addMemberCard').style.display = isOwner ? 'flex' : 'none';
}

async function getAuthHeaders() {
  const token = localStorage.getItem('cm_access_token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function addMember() {
  const userId = document.getElementById('newMemberId').value.trim();
  if (!userId) { showToast('Enter a user ID.', true); return; }
  if (!currentUserId) { showToast('Set your user ID first.', true); return; }

  const authHeaders = await getAuthHeaders();
  if (!authHeaders.Authorization) {
    showToast('Please login again.', true);
    return;
  }

  try {
    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}/members`,
      {
        method: 'POST',
        headers: { ...authHeaders, 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId }),
      }
    );

    const data = await res.json().catch(() => ({}));
    if (!res.ok) { showToast(data.error ?? 'Failed to add member.', true); return; }

    document.getElementById('newMemberId').value = '';
    showToast('Member added.');
    loadTeam();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

async function removeMember(targetUserId) {
  if (!confirm('Remove this member from the team?')) return;

  const authHeaders = await getAuthHeaders();
  if (!authHeaders.Authorization) {
    showToast('Please login again.', true);
    return;
  }

  try {
    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}/members/${encodeURIComponent(targetUserId)}`,
      { method: 'DELETE', headers: authHeaders }
    );

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Failed to remove member.', true);
      return;
    }

    showToast('Member removed.');
    loadTeam();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

async function loadImages() {
  const res = await fetch(`${API_BASE}/api/teams/${encodeURIComponent(teamId)}/images`);
  const images = await res.json();
  renderImages(images);
}

function renderImages(images) {
  const grid = document.getElementById('imagesGrid');
  if (!images.length) {
    grid.innerHTML = `<p style="color:var(--text-muted);font-size:0.85rem;grid-column:1/-1">No images yet.</p>`;
    return;
  }
  grid.innerHTML = images.map(img => `
    <div class="image-card">
      <img src="${API_BASE}${esc(img.imageUrl)}" alt="Team image" loading="lazy" />
      <div class="image-card-info">
        <p title="${esc(img.notes ?? '')}">${esc(img.notes ?? 'No notes')}</p>
        <span>By ${esc(img.uploadedBy)} · ${new Date(img.uploadedAt).toLocaleDateString()}</span>
      </div>
    </div>`).join('');
}

async function uploadImage() {
  if (!currentUserId) { showToast('Set your user ID first.', true); return; }

  const fileInput = document.getElementById('imageFile');
  const notes = document.getElementById('imageNotes').value.trim();

  if (!fileInput.files.length) { showToast('Select a file first.', true); return; }

  const formData = new FormData();
  formData.append('file', fileInput.files[0]);
  if (notes) formData.append('notes', notes);

  try {
    const params = new URLSearchParams({ userId: currentUserId });
    if (notes) params.append('notes', notes);

    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}/images?${params.toString()}`,
      { method: 'POST', body: formData }
    );

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Upload failed.', true);
      return;
    }

    fileInput.value = '';
    document.getElementById('imageNotes').value = '';
    showToast('Image uploaded!');
    loadImages();
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

function attachAuthButton() {
  const authBtn = document.getElementById('authBtn');
  if (!authBtn) return;

  authBtn.textContent = currentUserId ? 'Logout' : 'Login';
  authBtn.addEventListener('click', () => {
    if (currentUserId) {
      localStorage.removeItem('cm_user_id');
      localStorage.removeItem('cm_access_token');
      localStorage.removeItem('cm_user');
      currentUserId = '';
      currentUserName = '';
      document.getElementById('navUserLabel').textContent = '';
      showToast('Logged out.');
      window.location.href = 'teams.html';
    } else {
      const nextUrl = encodeURIComponent(`team-detail.html?id=${teamId}`);
      window.location.href = `login.html?next=${nextUrl}`;
    }
  });
}

function esc(str) {
  const d = document.createElement('div');
  d.textContent = String(str);
  return d.innerHTML;
}
