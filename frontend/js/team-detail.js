const params = new URLSearchParams(window.location.search);
const teamId = params.get('id');
let currentTeam = null;

if (!teamId) window.location.href = 'teams.html';

async function loadTeam() {
    try {
        const res = await fetch(`${API_BASE}/api/teams/${teamId}`);
        if (!res.ok) { window.location.href = 'teams.html'; return; }

        currentTeam = await res.json();
        renderTeamHeader(currentTeam);
        renderMembers(currentTeam);
        await loadImages();

        document.title = `${currentTeam.name} — NEXORA`;
        document.getElementById('team-header-container').innerHTML = '';
        document.getElementById('team-content').style.display = 'block';
    } catch {
        showToast('Could not load team.', true);
    }
}

function renderTeamHeader(team) {
    document.getElementById('team-header-container').innerHTML = `
        <div class="team-header">
            <h1>${escapeHtml(team.name)}</h1>
            <p>${escapeHtml(team.description ?? 'No description.')}</p>
        </div>`;
}

function renderMembers(team) {
    const isOwner = team.createdBy === CURRENT_USER_ID;
    const list = document.getElementById('members-list');

    list.innerHTML = team.members.map(m => {
        const initials = m.userId.substring(0, 2).toUpperCase();
        const joined = new Date(m.joinedAt).toLocaleDateString();
        const canRemove = isOwner && m.userId !== CURRENT_USER_ID;

        return `
            <div class="member-row">
                <div class="member-info">
                    <div class="avatar">${initials}</div>
                    <div>
                        <div class="member-name">${escapeHtml(m.userId)}</div>
                        <div class="member-joined">Joined ${joined}</div>
                    </div>
                </div>
                <div style="display:flex;align-items:center;gap:10px">
                    <span class="role-badge role-${m.role}">${m.role}</span>
                    ${canRemove ? `<button class="btn btn-danger" onclick="removeMember('${m.userId}')">Remove</button>` : ''}
                </div>
            </div>`;
    }).join('');

    // Show add member form only to owner
    const addCard = document.getElementById('add-member-card');
    addCard.style.display = isOwner ? 'block' : 'none';
}

async function addMember() {
    const userId = document.getElementById('new-member-id').value.trim();
    if (!userId) { showToast('Enter a user ID.', true); return; }

    try {
        const res = await fetch(`${API_BASE}/api/teams/${teamId}/members?userId=${CURRENT_USER_ID}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId })
        });

        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
            showToast(data.error ?? 'Failed to add member.', true);
            return;
        }

        document.getElementById('new-member-id').value = '';
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
            `${API_BASE}/api/teams/${teamId}/members/${targetUserId}?userId=${CURRENT_USER_ID}`,
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

async function loadImages() {
    const res = await fetch(`${API_BASE}/api/teams/${teamId}/images`);
    const images = await res.json();
    renderImages(images);
}

function renderImages(images) {
    const grid = document.getElementById('images-grid');
    if (!images.length) {
        grid.innerHTML = `<p style="color:#475569;font-size:0.9rem">No images uploaded yet.</p>`;
        return;
    }
    grid.innerHTML = images.map(img => `
        <div class="image-card">
            <img src="${API_BASE}${img.imageUrl}" alt="Team image">
            <div class="image-card-info">
                <p>${escapeHtml(img.notes ?? 'No notes')}</p>
                <span>By ${escapeHtml(img.uploadedBy)} · ${new Date(img.uploadedAt).toLocaleDateString()}</span>
            </div>
        </div>`).join('');
}

async function uploadImage() {
    const fileInput = document.getElementById('image-file');
    const notes = document.getElementById('image-notes').value.trim();

    if (!fileInput.files.length) {
        showToast('Select a file first.', true);
        return;
    }

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);
    if (notes) formData.append('notes', notes);

    try {
        const res = await fetch(
            `${API_BASE}/api/teams/${teamId}/images?userId=${CURRENT_USER_ID}`,
            { method: 'POST', body: formData }
        );

        if (!res.ok) {
            const data = await res.json().catch(() => ({}));
            showToast(data.error ?? 'Upload failed.', true);
            return;
        }

        fileInput.value = '';
        document.getElementById('image-notes').value = '';
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
    setTimeout(() => toast.classList.remove('show'), 3000);
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

loadTeam();
