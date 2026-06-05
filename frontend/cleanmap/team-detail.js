// ---- Config ----
const API_CANDIDATES = [
  'http://localhost:5432',
  'http://localhost:30543',
  'http://localhost:5210',
  'http://localhost:7210',
  'http://localhost:5000',
  'http://localhost:5001',
];

const params = new URLSearchParams(window.location.search);
const teamId = params.get('id');
if (!teamId) window.location.href = 'teams.html';

let API_BASE = localStorage.getItem('cm_api_base') || '';
let currentUserId = localStorage.getItem('cm_user_id') || '';
let currentTeam = null;

// ---- Init ----
document.getElementById('navUserLabel').textContent =
  currentUserId ? `You: ${currentUserId}` : '';

detectApiAndLoad();

// ---- API detection ----
async function detectApiAndLoad() {
  const candidates = API_CANDIDATES.slice();
  if (API_BASE && !candidates.includes(API_BASE)) candidates.unshift(API_BASE);

  let found = '';
  for (const base of candidates) {
    try {
      const res = await fetch(`${base}/api/cleanmap/health`, { signal: AbortSignal.timeout(2000) });
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
  loadTeam();
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

    document.title = `${currentTeam.name} — CleanMap`;
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

    return `
      <div class="member-row">
        <div class="member-info">
          <div class="avatar">${initials}</div>
          <div>
            <div class="member-name">${esc(m.userId)}</div>
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

async function addMember() {
  const userId = document.getElementById('newMemberId').value.trim();
  if (!userId) { showToast('Enter a user ID.', true); return; }
  if (!currentUserId) { showToast('Set your user ID first.', true); return; }

  try {
    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}/members?userId=${encodeURIComponent(currentUserId)}`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
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

  try {
    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}/members/${encodeURIComponent(targetUserId)}?userId=${encodeURIComponent(currentUserId)}`,
      { method: 'DELETE' }
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

// ---- Images ----
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
    const res = await fetch(
      `${API_BASE}/api/teams/${encodeURIComponent(teamId)}/images?userId=${encodeURIComponent(currentUserId)}`,
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

// ---- Helpers ----
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
