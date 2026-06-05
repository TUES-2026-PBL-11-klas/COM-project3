async function loadTeams() {
    try {
        const res = await fetch(`${API_BASE}/api/teams/my?userId=${CURRENT_USER_ID}`);
        const teams = await res.json();
        renderTeams(teams);
    } catch {
        document.getElementById('teams-container').innerHTML =
            `<div class="empty-state"><p>Could not connect to API.</p></div>`;
    }
}

function renderTeams(teams) {
    const container = document.getElementById('teams-container');

    if (!teams.length) {
        container.innerHTML = `
            <div class="empty-state">
                <svg width="48" height="48" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
                    <path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
                <p>You are not part of any team yet.</p>
            </div>`;
        return;
    }

    container.innerHTML = `<div class="teams-grid">${teams.map(teamCard).join('')}</div>`;
}

function teamCard(team) {
    const isOwner = team.createdBy === CURRENT_USER_ID;
    const role = isOwner ? 'owner' : 'member';

    return `
        <div class="team-card" onclick="window.location.href='team-detail.html?id=${team.id}'">
            <h3>${escapeHtml(team.name)}</h3>
            <p>${escapeHtml(team.description ?? 'No description.')}</p>
            <div class="team-card-footer">
                <span class="member-count">${team.members.length} member${team.members.length !== 1 ? 's' : ''}</span>
                <span class="role-badge role-${role}">${role}</span>
                ${isOwner ? `<button class="btn-delete-team" onclick="event.stopPropagation(); deleteTeam('${team.id}', '${escapeHtml(team.name)}')">&times; Delete</button>` : ''}
            </div>
        </div>`;
}

async function deleteTeam(teamId, teamName) {
    if (!confirm(`Delete team "${teamName}"? This cannot be undone.`)) return;

    try {
        const res = await fetch(`${API_BASE}/api/teams/${teamId}?userId=${CURRENT_USER_ID}`, {
            method: 'DELETE'
        });

        if (!res.ok) {
            const data = await res.json();
            showToast(data.error ?? 'Failed to delete team.', true);
            return;
        }

        showToast('Team deleted.');
        loadTeams();
    } catch {
        showToast('Could not connect to API.', true);
    }
}

function openCreateModal() {
    document.getElementById('create-modal').classList.add('open');
    document.getElementById('team-name').focus();
}

function closeCreateModal() {
    document.getElementById('create-modal').classList.remove('open');
    document.getElementById('team-name').value = '';
    document.getElementById('team-desc').value = '';
}

async function createTeam() {
    const name = document.getElementById('team-name').value.trim();
    const description = document.getElementById('team-desc').value.trim();

    if (!name) {
        showToast('Team name is required.', true);
        return;
    }

    try {
        const res = await fetch(`${API_BASE}/api/teams?userId=${CURRENT_USER_ID}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, description })
        });

        if (!res.ok) {
            const data = await res.json();
            showToast(data.error ?? 'Failed to create team.', true);
            return;
        }

        closeCreateModal();
        showToast('Team created successfully!');
        loadTeams();
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

// Close modal on overlay click
document.getElementById('create-modal').addEventListener('click', function (e) {
    if (e.target === this) closeCreateModal();
});

loadTeams();
