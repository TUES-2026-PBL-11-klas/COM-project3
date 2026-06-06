const storageKey = 'cleanmap_v1';

const state = {
  db: { reports: [], lastCenter: null, lastZoom: 13 },
  map: null,
  markers: new Map(),
  currentStream: null,
  reportPhoto: null,
  cleanPhoto: null,
  currentLocation: null,
  activeReportId: null,
  userMarker: null,
  apiBase: null,
  apiEnabled: false,
  clusterLayers: [],
  // Auth state
  accessToken: null,
  currentUser: null
};

const elements = {
  reportBtn: document.getElementById('reportBtn'),
  reportBtnBottom: document.getElementById('reportBtnBottom'),
  reviewBtn: document.getElementById('reviewBtn'),
  locateBtn: document.getElementById('locateBtn'),
  progressText: document.getElementById('progressText'),
  progressFill: document.getElementById('progressFill'),
  progressFillBottom: document.getElementById('progressFillBottom'),
  totalCount: document.getElementById('totalCount'),
  cleanedCount: document.getElementById('cleanedCount'),
  dirtyCount: document.getElementById('dirtyCount'),
  reportModal: document.getElementById('reportModal'),
  cleanModal: document.getElementById('cleanModal'),
  detailModal: document.getElementById('detailModal'),
  coordText: document.getElementById('coordText'),
  addressInput: document.getElementById('addressDisplay'),
  addressBtn: null,
  notesInput: document.getElementById('notesInput'),
  reportVideo: document.getElementById('reportVideo'),
  reportPreview: document.getElementById('reportPreview'),
  captureReport: document.getElementById('captureReport'),
  retakeReport: document.getElementById('retakeReport'),
  submitReport: document.getElementById('submitReport'),
  cancelReport: document.getElementById('cancelReport'),
  closeReport: document.getElementById('closeReport'),
  detailTitle: document.getElementById('detailTitle'),
  detailStatus: document.getElementById('detailStatus'),
  detailBefore: document.getElementById('detailBefore'),
  detailAfter: document.getElementById('detailAfter'),
  detailCoords: document.getElementById('detailCoords'),
  detailAddress: document.getElementById('detailAddress'),
  detailTags: document.getElementById('detailTags'),
  detailDate: document.getElementById('detailDate'),
  detailCleaned: document.getElementById('detailCleaned'),
  detailCleanedDate: document.getElementById('detailCleanedDate'),
  afterBlock: document.getElementById('afterBlock'),
  markClean: document.getElementById('markClean'),
  closeDetail: document.getElementById('closeDetail'),
  cleanVideo: document.getElementById('cleanVideo'),
  cleanPreview: document.getElementById('cleanPreview'),
  captureClean: document.getElementById('captureClean'),
  retakeClean: document.getElementById('retakeClean'),
  submitClean: document.getElementById('submitClean'),
  cancelClean: document.getElementById('cancelClean'),
  closeClean: document.getElementById('closeClean'),
  toast: document.getElementById('toast'),
  reviewPanel: document.getElementById('reviewPanel'),
  closeReview: document.getElementById('closeReview'),
  reviewList: document.getElementById('reviewList'),
  reviewFilterAll: document.getElementById('reviewFilterAll'),
  reviewFilterNearby: document.getElementById('reviewFilterNearby'),
  photosModal: document.getElementById('photosModal'),
  closePhotos: document.getElementById('closePhotos'),
  photosBefore: document.getElementById('photosBefore'),
  photosAfter: document.getElementById('photosAfter'),
  photosAfterPane: document.getElementById('photosAfterPane'),
  
  // Auth Elements
  loginBtn: document.getElementById('loginBtn'),
  userIndicator: document.getElementById('userIndicator'),
  userAvatar: document.getElementById('userAvatar'),
  userName: document.getElementById('userName'),
  logoutBtn: document.getElementById('logoutBtn'),
  authModal: document.getElementById('authModal'),
  tabLogin: document.getElementById('tabLogin'),
  tabSignup: document.getElementById('tabSignup'),
  loginForm: document.getElementById('loginForm'),
  signupForm: document.getElementById('signupForm'),
  loginEmail: document.getElementById('loginEmail'),
  loginPassword: document.getElementById('loginPassword'),
  signupUsername: document.getElementById('signupUsername'),
  signupEmail: document.getElementById('signupEmail'),
  signupPassword: document.getElementById('signupPassword'),
  loginError: document.getElementById('loginError'),
  signupError: document.getElementById('signupError'),
  submitLogin: document.getElementById('submitLogin'),
  submitSignup: document.getElementById('submitSignup'),
  closeAuth: document.getElementById('closeAuth')
};

function loadDB() {
  const raw = localStorage.getItem(storageKey);
  if (!raw) return { reports: [], lastCenter: null, lastZoom: 13 };
  try {
    const parsed = JSON.parse(raw);
    if (!parsed.reports) parsed.reports = [];
    return parsed;
  } catch (err) {
    return { reports: [], lastCenter: null, lastZoom: 13 };
  }
}

function saveDB() {
  localStorage.setItem(storageKey, JSON.stringify(state.db));
}

function showToast(message, isError = false) {
  elements.toast.textContent = message;
  elements.toast.className = 'toast show' + (isError ? ' error' : '');
  clearTimeout(elements.toast._timer);
  elements.toast._timer = setTimeout(() => elements.toast.classList.remove('show'), 3000);
}

function getAuthHeaders() {
  const headers = {
    'Content-Type': 'application/json'
  };

  if (state.auth.token) {
    headers.Authorization = `Bearer ${state.auth.token}`;
  }

  return headers;
}

function authFetch(url, options = {}, timeoutMs = 6000) {
  if (!state.auth.token) {
    return Promise.reject(new Error('Login required.'));
  }

  return fetchJson(url, {
    ...options,
    headers: {
      ...getAuthHeaders(),
      ...(options.headers || {})
    }
  }, timeoutMs);
}

function isAuthenticated() {
  return !!state.auth.token && !!state.auth.user;
}

function updateAuthUi() {
  const authBtn = document.getElementById('authBtn');
  if (!authBtn) return;

  if (isAuthenticated()) {
    authBtn.textContent = 'Logout';
  } else {
    authBtn.textContent = 'Login';
  }
}

function redirectToLogin(next = 'login.html') {
  window.location.href = `login.html?next=${encodeURIComponent(next)}`;
}

function requireAuth(action) {
  if (!isAuthenticated()) {
    showToast('Login required before continuing.', true);
    setTimeout(() => redirectToLogin(action), 800);
    return false;
  }
  return true;
}

function buildApiCandidates() {
  const stored = localStorage.getItem(`${storageKey}_api`);
  const preferred = 'https://com-project3.onrender.com';
  const isLocal = location.hostname === 'localhost' || location.hostname === '127.0.0.1';
  const storedIsLocal = stored && (stored.includes('localhost') || stored.includes('127.0.0.1'));
  // Always try the current origin first — nginx proxies /api/ to the backend
  const candidates = [
    window.location.origin,
    storedIsLocal && !isLocal ? null : stored,
    preferred,
    ...(isLocal ? [
      'http://localhost:5432',
      'http://localhost:30543',
      'http://localhost:5210',
      'https://localhost:7210',
      'http://localhost:5000',
      'https://localhost:5001'
    ] : [])
  ];
  return Array.from(new Set(candidates.filter(c => c && c !== 'null')));
}

async function fetchJson(url, options = {}, timeoutMs = 8000) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  
  // Setup headers
  const headers = new Headers(options.headers || {});
  
  // Attach Access Token if available
  if (state.accessToken) {
    headers.set('Authorization', `Bearer ${state.accessToken}`);
  }
  
  // Attach CSRF Token for state-changing requests
  const method = (options.method || 'GET').toUpperCase();
  if (method !== 'GET' && method !== 'HEAD') {
    const xsrfCookie = document.cookie.split('; ').find(row => row.startsWith('XSRF-TOKEN='));
    if (xsrfCookie) {
      headers.set('X-XSRF-TOKEN', decodeURIComponent(xsrfCookie.split('=')[1]));
    }
  }

  // Ensure credentials (cookies) are included
  options.credentials = 'include';
  options.headers = headers;

  try {
    const response = await fetch(url, { ...options, signal: controller.signal });
    
    // Handle 401 Unauthorized globally
    if (response.status === 401 && !url.includes('/auth/refresh')) {
      const refreshed = await auth.refresh();
      if (refreshed) {
        // Retry the original request with the new token
        headers.set('Authorization', `Bearer ${state.accessToken}`);
        const retryResponse = await fetch(url, { ...options, headers, signal: controller.signal });
        return handleFetchResponse(retryResponse);
      } else {
        auth.showLogin();
        throw new Error('Unauthorized');
      }
    }
    
    return handleFetchResponse(response);
  } finally {
    clearTimeout(timeoutId);
  }
}

async function handleFetchResponse(response) {
  const contentType = response.headers.get('content-type') || '';
  if (!response.ok) {
    let errorMsg = `HTTP ${response.status}`;
    if (contentType.includes('application/json')) {
      const errBody = await response.json();
      if (errBody.error) errorMsg = errBody.error;
    }
    throw new Error(errorMsg);
  }
  
  if (contentType.includes('application/json')) {
    return await response.json();
  }
  return null;
}

async function detectApiBase() {
  const candidates = buildApiCandidates();
  for (const base of candidates) {
    try {
      await fetchJson(`${base}/api/cleanmap/ping`, {}, 6000);
      state.apiBase = base;
      state.apiEnabled = true;
      return true;
    } catch (err) {
      continue;
    }
  }
  state.apiEnabled = false;
  state.apiBase = null;
  return false;
}

async function syncFromApi() {
  const detected = await detectApiBase();
  if (!detected) return;
  try {
    const reports = await fetchJson(`${state.apiBase}/api/cleanmap/reports`);
    if (Array.isArray(reports)) {
      state.db.reports = reports;
      saveDB();
      renderMarkers();
      updateProgress();
    }
  } catch (err) {
    state.apiEnabled = false;
    state.apiBase = null;
  }
}

async function createReportApi(report) {
  return authFetch(`${state.apiBase}/api/cleanmap/reports`, {
    method: 'POST',
    body: JSON.stringify(report)
  }, 30000);
}

async function markCleanApi(reportId, payload) {
  return authFetch(`${state.apiBase}/api/cleanmap/reports/${encodeURIComponent(reportId)}/clean`, {
    method: 'POST',
    body: JSON.stringify(payload)
  }, 30000);
}

// --- Auth Module ---
const auth = {
  refreshTimer: null,

  updateUI() {
    if (state.currentUser) {
      elements.userIndicator.classList.remove('hidden');
      elements.loginBtn.classList.add('hidden');
      elements.userName.textContent = state.currentUser.username;
      elements.userAvatar.textContent = state.currentUser.username.substring(0, 1).toUpperCase();
      
      // Enable reporting features
      elements.reportBtn.classList.remove('hidden');
    } else {
      elements.userIndicator.classList.add('hidden');
      elements.loginBtn.classList.remove('hidden');
      elements.reportBtn.classList.add('hidden');
    }
  },

  showLogin() {
    elements.loginError.textContent = '';
    elements.signupError.textContent = '';
    elements.loginPassword.value = '';
    elements.signupPassword.value = '';
    openModal(elements.authModal);
  },

  hideAuth() {
    closeModal(elements.authModal);
  },

  switchTab(tab) {
    if (tab === 'login') {
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
  },

  scheduleRefresh() {
    if (this.refreshTimer) clearTimeout(this.refreshTimer);
    // Refresh 1 minute before the 15-minute expiry (14 minutes)
    this.refreshTimer = setTimeout(() => this.refresh(), 14 * 60 * 1000);
  },

  async handleAuthResponse(data) {
    if (data && data.accessToken && data.user) {
      state.accessToken = data.accessToken;
      state.currentUser = data.user;
      this.updateUI();
      this.hideAuth();
      this.scheduleRefresh();
      return true;
    }
    return false;
  },

  async login(email, password) {
    elements.submitLogin.disabled = true;
    elements.loginError.textContent = '';
    try {
      const data = await fetchJson(`${state.apiBase}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      await this.handleAuthResponse(data);
      showToast(`Welcome back, ${state.currentUser.username}!`);
    } catch (err) {
      elements.loginError.textContent = err.message || 'Login failed.';
    } finally {
      elements.submitLogin.disabled = false;
    }
  },

  async signup(username, email, password) {
    elements.submitSignup.disabled = true;
    elements.signupError.textContent = '';
    try {
      const data = await fetchJson(`${state.apiBase}/api/auth/signup`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, email, password })
      });
      // Signup returns token but not refresh cookie. We need them to login.
      showToast('Account created! Please log in.');
      this.switchTab('login');
      elements.loginEmail.value = email;
      elements.loginPassword.value = '';
    } catch (err) {
      elements.signupError.textContent = err.message || 'Signup failed.';
    } finally {
      elements.submitSignup.disabled = false;
    }
  },

  async refresh() {
    if (!state.apiEnabled) return false;
    try {
      const data = await fetchJson(`${state.apiBase}/api/auth/refresh`, { method: 'POST' });
      return await this.handleAuthResponse(data);
    } catch (err) {
      state.accessToken = null;
      state.currentUser = null;
      this.updateUI();
      return false;
    }
  },

  async logout() {
    try {
      await fetchJson(`${state.apiBase}/api/auth/logout`, { method: 'POST' });
    } catch (e) {
      // Ignore errors on logout
    }
    if (this.refreshTimer) clearTimeout(this.refreshTimer);
    state.accessToken = null;
    state.currentUser = null;
    this.updateUI();
    showToast('Logged out successfully.');
  }
};

function initMap() {
  const defaultCenter = state.db.lastCenter || [42.6977, 23.3219];
  state.map = L.map('map', {
    zoomControl: false,
    minZoom: 3,
    maxZoom: 19
  }).setView(defaultCenter, state.db.lastZoom || 13);

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors',
    subdomains: 'abc'
  }).addTo(state.map);

  L.control.zoom({ position: 'bottomright' }).addTo(state.map);

  state.map.on('moveend', () => {
    const center = state.map.getCenter();
    state.db.lastCenter = [center.lat, center.lng];
    state.db.lastZoom = state.map.getZoom();
    saveDB();
  });
}

function makeIcon(status) {
  const cls = status === 'cleaned' ? 'marker-clean' : 'marker-dirty';
  return L.divIcon({
    className: 'cleanmap-marker',
    html: `<div class="marker ${cls}"><span class="marker-badge"></span></div>`,
    iconSize: [28, 38],
    iconAnchor: [14, 28]
  });
}

function addMarker(report) {
  const zIndex = report.status === 'cleaned' ? 100 : 500;
  const marker = L.marker([report.lat, report.lng], {
    icon: makeIcon(report.status),
    zIndexOffset: zIndex
  }).addTo(state.map);
  marker.on('click', () => openDetail(report.id));
  state.markers.set(report.id, marker);
}

function haversineDistance(lat1, lng1, lat2, lng2) {
  const R = 6371000;
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLng = (lng2 - lng1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function computeClusters(reports) {
  const CLUSTER_RADIUS_M = 150;
  const MIN_CLUSTER_SIZE = 3;
  const dirty = reports.filter(r => r.status !== 'cleaned');
  const visited = new Set();
  const clusters = [];

  for (let i = 0; i < dirty.length; i++) {
    if (visited.has(i)) continue;
    const group = [i];
    for (let j = i + 1; j < dirty.length; j++) {
      if (visited.has(j)) continue;
      if (haversineDistance(dirty[i].lat, dirty[i].lng, dirty[j].lat, dirty[j].lng) <= CLUSTER_RADIUS_M) {
        group.push(j);
      }
    }
    if (group.length >= MIN_CLUSTER_SIZE) {
      group.forEach(idx => visited.add(idx));
      const avgLat = group.reduce((s, idx) => s + dirty[idx].lat, 0) / group.length;
      const avgLng = group.reduce((s, idx) => s + dirty[idx].lng, 0) / group.length;
      const maxDist = Math.max(...group.map(idx =>
        haversineDistance(avgLat, avgLng, dirty[idx].lat, dirty[idx].lng)));
      clusters.push({
        lat: avgLat,
        lng: avgLng,
        radius: Math.max(maxDist + 40, 80),
        ids: new Set(group.map(idx => dirty[idx].id))
      });
    }
  }

  const clusteredIds = new Set(clusters.flatMap(c => [...c.ids]));
  return { clusters, clusteredIds };
}

function renderMarkers() {
  state.markers.forEach(marker => marker.remove());
  state.markers.clear();
  state.clusterLayers.forEach(l => l.remove());
  state.clusterLayers = [];

  const { clusters, clusteredIds } = computeClusters(state.db.reports);

  clusters.forEach(cluster => {
    const circle = L.circle([cluster.lat, cluster.lng], {
      radius: cluster.radius,
      color: '#ff4d6d',
      fillColor: '#ff4d6d',
      fillOpacity: 0.25,
      weight: 2,
      opacity: 0.7
    }).addTo(state.map);
    state.clusterLayers.push(circle);
  });

  const ONE_WEEK_MS = 7 * 24 * 60 * 60 * 1000;
  const now = Date.now();

  state.db.reports.forEach(report => {
    if (clusteredIds.has(report.id)) return;
    if (report.status === 'cleaned') {
      const cleanedAt = report.cleanedAt || report.createdAt;
      if (now - cleanedAt > ONE_WEEK_MS) return;
    }
    addMarker(report);
  });
}

function updateProgress() {
  const total = state.db.reports.length;
  const cleaned = state.db.reports.filter(report => report.status === 'cleaned').length;
  const dirty = total - cleaned;
  const ratio = total === 0 ? 0 : cleaned / total;

  elements.progressText.textContent = `${cleaned}/${total}`;
  elements.progressFill.style.width = `${ratio * 100}%`;
  elements.progressFillBottom.style.width = `${ratio * 100}%`;
  elements.totalCount.textContent = total;
  elements.cleanedCount.textContent = cleaned;
  elements.dirtyCount.textContent = dirty;
}

async function reverseGeocode(lat, lng) {
  try {
    const response = await fetch(
      `https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${lat}&lon=${lng}`,
      { headers: { 'Accept-Language': 'en' }, signal: AbortSignal.timeout(5000) }
    );
    const data = await response.json();
    if (!data || !data.address) return null;
    const a = data.address;
    const parts = [
      a.road || a.pedestrian || a.footway || a.path,
      a.house_number,
      a.suburb || a.neighbourhood || a.quarter,
      a.city || a.town || a.village || a.municipality
    ].filter(Boolean);
    return parts.join(', ') || data.display_name || null;
  } catch {
    return null;
  }
}

function setLocation(lat, lng, panMap) {
  state.currentLocation = { lat, lng };
  elements.coordText.textContent = `${lat.toFixed(5)}, ${lng.toFixed(5)}`;
  if (panMap && state.map) state.map.setView([lat, lng], Math.max(state.map.getZoom(), 15));
  if (!state.userMarker) {
    state.userMarker = L.circleMarker([lat, lng], {
      radius: 6,
      color: '#40c9ff',
      fillColor: '#40c9ff',
      fillOpacity: 0.7
    }).addTo(state.map);
  } else {
    state.userMarker.setLatLng([lat, lng]);
  }

  const reportModalOpen = !elements.reportModal.classList.contains('hidden');
  if (reportModalOpen) {
    elements.addressInput.textContent = 'Detecting address...';
    reverseGeocode(lat, lng).then(addr => {
      elements.addressInput.textContent = addr || `${lat.toFixed(5)}, ${lng.toFixed(5)}`;
    });
  }
}

function locateUser(panMap) {
  if (!navigator.geolocation) {
    showToast('Geolocation is not supported. Using map center.');
    if (state.map) {
      const center = state.map.getCenter();
      setLocation(center.lat, center.lng, false);
    }
    return;
  }

  navigator.geolocation.getCurrentPosition(
    position => {
      setLocation(position.coords.latitude, position.coords.longitude, panMap);
    },
    () => {
      showToast('Unable to fetch location. Using map center.');
      if (state.map) {
        const center = state.map.getCenter();
        setLocation(center.lat, center.lng, false);
      }
    },
    { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 }
  );
}

async function attachCamera(videoEl) {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    showToast('Camera requires https or localhost.');
    return;
  }
  const isAlive = state.currentStream &&
    state.currentStream.getTracks().every(t => t.readyState === 'live');
  if (!isAlive) {
    try {
      state.currentStream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: { ideal: 'environment' } },
        audio: false
      });
    } catch {
      showToast('Camera unavailable.');
      return;
    }
  }
  videoEl.srcObject = state.currentStream;
  await videoEl.play().catch(() => {});
}

function releaseCamera(videoEl) {
  videoEl.pause();
  videoEl.srcObject = null;
}

function stopCamera() {
  if (state.currentStream) {
    state.currentStream.getTracks().forEach(track => track.stop());
    state.currentStream = null;
  }
}

function captureFrame(videoEl) {
  if (!videoEl.videoWidth || !videoEl.videoHeight) {
    showToast('Camera not ready yet.');
    return null;
  }
  const canvas = document.createElement('canvas');
  canvas.width = videoEl.videoWidth || 1280;
  canvas.height = videoEl.videoHeight || 720;
  const ctx = canvas.getContext('2d');
  ctx.drawImage(videoEl, 0, 0, canvas.width, canvas.height);
  return canvas.toDataURL('image/jpeg', 0.85);
}

function openModal(el) {
  el.classList.remove('hidden');
  el.setAttribute('aria-hidden', 'false');
}

function closeModal(el) {
  el.classList.add('hidden');
  el.setAttribute('aria-hidden', 'true');
}

function resetReportForm() {
  state.reportPhoto = null;
  state.currentLocation = null;
  elements.reportPreview.classList.add('hidden');
  elements.reportVideo.classList.remove('hidden');
  elements.retakeReport.classList.add('hidden');
  elements.captureReport.classList.remove('hidden');
  elements.notesInput.value = '';
  elements.addressInput.textContent = 'Detecting address...';

  elements.addressInput.textContent = 'Detecting address...';
  elements.coordText.textContent = 'Locating...';
}

function ensureLocationForReport() {
  if (state.currentLocation) return true;
  if (state.map) {
    const center = state.map.getCenter();
    setLocation(center.lat, center.lng, false);
    return true;
  }
  return false;
}

function openReportModal() {
  if (!requireAuth('login.html')) return;
  resetReportForm();
  openModal(elements.reportModal);
  locateUser(true);
  void attachCamera(elements.reportVideo);
}

function closeReportModal() {
  releaseCamera(elements.reportVideo);
  closeModal(elements.reportModal);
}

function openDetail(reportId) {
  const report = state.db.reports.find(item => item.id === reportId);
  if (!report) return;

  elements.detailTitle.textContent = report.address || 'Pollution Report';
  elements.detailStatus.textContent = `Status: ${report.status === 'cleaned' ? 'Cleaned' : 'Dirty'}`;
  elements.detailBefore.src = report.photoBefore;
  elements.detailCoords.textContent = `${report.lat.toFixed(5)}, ${report.lng.toFixed(5)}`;
  elements.detailAddress.textContent = report.address || 'No address provided';
  elements.detailTags.textContent = report.tags.length ? report.tags.join(', ') : 'None';
  elements.detailDate.textContent = formatDate(report.createdAt);

  if (report.status === 'cleaned' && report.photoAfter) {
    elements.afterBlock.classList.remove('hidden');
    elements.detailAfter.src = report.photoAfter;
    elements.detailCleaned.classList.remove('hidden');
    elements.detailCleanedDate.textContent = formatDate(report.cleanedAt);
    elements.markClean.classList.add('hidden');
  } else {
    elements.afterBlock.classList.add('hidden');
    elements.detailCleaned.classList.add('hidden');
    elements.markClean.classList.remove('hidden');
    
    // Disable clean button if not logged in
    if (!state.currentUser) {
      elements.markClean.disabled = true;
      elements.markClean.title = "Login required to mark as cleaned";
      elements.markClean.classList.add('ghost');
      elements.markClean.classList.remove('primary');
    } else {
      elements.markClean.disabled = false;
      elements.markClean.title = "";
      elements.markClean.classList.remove('ghost');
      elements.markClean.classList.add('primary');
    }
    
    elements.markClean.dataset.reportId = report.id;
  }

  openModal(elements.detailModal);
}

function closeDetailModal() {
  closeModal(elements.detailModal);
}

function openPhotosModal(report) {
  elements.photosBefore.src = report.photoBefore || '';
  if (report.photoAfter) {
    elements.photosAfter.src = report.photoAfter;
    elements.photosAfterPane.classList.remove('hidden');
  } else {
    elements.photosAfterPane.classList.add('hidden');
  }
  openModal(elements.photosModal);
  const scroll = elements.photosModal.querySelector('.photos-scroll');
  if (scroll) scroll.scrollTop = 0;
}

function closePhotosModal() {
  closeModal(elements.photosModal);
}

let reviewFilter = 'all';

function openReviewPanel() {
  renderReviewList();
  openModal(elements.reviewPanel);
}

function closeReviewPanel() {
  closeModal(elements.reviewPanel);
}

function renderReviewList() {
  let reports = [...state.db.reports].sort((a, b) => b.createdAt - a.createdAt);

  if (reviewFilter === 'nearby' && state.currentLocation) {
    reports = reports.filter(r =>
      haversineDistance(state.currentLocation.lat, state.currentLocation.lng, r.lat, r.lng) <= 1000
    );
  }

  elements.reviewList.innerHTML = '';

  if (reports.length === 0) {
    elements.reviewList.innerHTML = '<div class="review-empty">No reports found.</div>';
    return;
  }

  reports.forEach(report => {
    const item = document.createElement('div');
    item.className = 'review-item';

    const distM = state.currentLocation
      ? Math.round(haversineDistance(state.currentLocation.lat, state.currentLocation.lng, report.lat, report.lng))
      : null;

    item.innerHTML = `
      <img class="review-thumb" src="" alt="">
      <div class="review-info">
        <div class="review-title"></div>
        <div class="review-meta tags-meta"></div>
        <div class="review-meta date-meta"></div>
      </div>
      <span class="review-badge"></span>
    `;

    item.querySelector('img').src = (report.status === 'cleaned' ? report.photoAfter : null) || report.photoBefore || '';
    item.querySelector('.review-title').textContent = report.address || 'No address';
    item.querySelector('.tags-meta').textContent =
      (distM !== null ? `${distM}m away · ` : '') + (report.tags?.join(', ') || 'No tags');
    item.querySelector('.date-meta').textContent = formatDate(report.createdAt);

    const badge = item.querySelector('.review-badge');
    badge.textContent = report.status === 'cleaned' ? 'Cleaned' : 'Dirty';
    badge.classList.add(report.status);

    item.addEventListener('click', () => {
      closeReviewPanel();
      openDetail(report.id);
    });

    elements.reviewList.appendChild(item);
  });
}

function openCleanModal(reportId) {
  if (!requireAuth('login.html')) return;

  const report = state.db.reports.find(r => r.id === reportId);
  if (!report) return;

  const proceed = (loc) => {
    const dist = haversineDistance(loc.lat, loc.lng, report.lat, report.lng);
    if (dist > 100) {
      showToast(`Too far from report (${Math.round(dist)}m away). Must be within 20m.`);
      return;
    }
    state.activeReportId = reportId;
    state.cleanPhoto = null;
    elements.cleanPreview.classList.add('hidden');
    elements.cleanVideo.classList.remove('hidden');
    elements.retakeClean.classList.add('hidden');
    elements.captureClean.classList.remove('hidden');
    openModal(elements.cleanModal);
    void attachCamera(elements.cleanVideo);
  };

  if (state.currentLocation) {
    proceed(state.currentLocation);
    return;
  }

  if (!navigator.geolocation) {
    showToast('Geolocation not supported.');
    return;
  }

  showToast('Getting your location...');
  navigator.geolocation.getCurrentPosition(
    pos => {
      const loc = { lat: pos.coords.latitude, lng: pos.coords.longitude };
      setLocation(loc.lat, loc.lng, false);
      proceed(loc);
    },
    () => showToast('Location unavailable. Enable GPS and try again.'),
    { enableHighAccuracy: true, timeout: 8000, maximumAge: 10000 }
  );
}

function closeCleanModal() {
  releaseCamera(elements.cleanVideo);
  closeModal(elements.cleanModal);
  state.activeReportId = null;
}

function formatDate(timestamp) {
  return new Date(timestamp).toLocaleString();
}

async function submitReport() {
  if (!ensureLocationForReport()) {
    showToast('Location not ready yet.');
    return;
  }

  if (!state.reportPhoto) {
    showToast('Capture a photo before submitting.');
    return;
  }

  const address = elements.addressInput.textContent.trim() === 'Detecting address...' ? '' : elements.addressInput.textContent.trim();
  const payload = {
    lat: state.currentLocation.lat,
    lng: state.currentLocation.lng,
    address,
    notes: elements.notesInput.value.trim(),
    photoBefore: state.reportPhoto
  };

  if (!state.apiEnabled) {
    const found = await detectApiBase();
    if (!found) {
      showToast('Server not reachable.');
      return;
    }
  }

  try {
    await createReportApi(payload);
  } catch (err) {
    showToast(err.message || 'Server not reachable.');
    return;
  }

  closeReportModal();
  await syncFromApi();
  showToast('Report saved.');
}

async function submitClean() {
  if (!state.cleanPhoto || !state.activeReportId) {
    showToast('Capture an after photo.');
    return;
  }

  const report = state.db.reports.find(item => item.id === state.activeReportId);
  if (!report) return;

  if (state.apiEnabled) {
    try {
      await markCleanApi(report.id, { photoAfter: state.cleanPhoto });
      closeCleanModal();
      closeDetailModal();
      await syncFromApi();
      showToast('Marked as cleaned.');
      return;
    } catch (err) {
      if (err.message && !err.message.includes('fetch') && !err.message.includes('NetworkError')) {
        showToast(err.message);
        return;
      }
      state.apiEnabled = false;
      state.apiBase = null;
      showToast('Server not reachable. Saved locally.');
    }
  }

  report.status = 'cleaned';
  report.photoAfter = state.cleanPhoto;
  report.cleanedAt = Date.now();

  saveDB();

  const marker = state.markers.get(report.id);
  if (marker) { marker.remove(); state.markers.delete(report.id); }

  updateProgress();
  closeCleanModal();
  closeDetailModal();
  showToast('Marked as cleaned.');
}

function bindUI() {
  elements.reportBtn.addEventListener('click', openReportModal);
  elements.reportBtnBottom.addEventListener('click', openReportModal);
  elements.locateBtn.addEventListener('click', () => locateUser(true));
  elements.reviewBtn.addEventListener('click', openReviewPanel);
  elements.closeReview.addEventListener('click', closeReviewPanel);
  elements.reviewFilterAll.addEventListener('click', () => {
    reviewFilter = 'all';
    elements.reviewFilterAll.classList.add('sort-active');
    elements.reviewFilterNearby.classList.remove('sort-active');
    renderReviewList();
  });
  elements.reviewFilterNearby.addEventListener('click', () => {
    if (!state.currentLocation) {
      if (!navigator.geolocation) {
        showToast('Geolocation not supported.');
        return;
      }
      showToast('Getting your location...');
      navigator.geolocation.getCurrentPosition(
        pos => {
          setLocation(pos.coords.latitude, pos.coords.longitude, false);
          reviewFilter = 'nearby';
          elements.reviewFilterNearby.classList.add('sort-active');
          elements.reviewFilterAll.classList.remove('sort-active');
          renderReviewList();
        },
        () => showToast('Location unavailable. Enable GPS and try again.'),
        { enableHighAccuracy: true, timeout: 8000, maximumAge: 10000 }
      );
      return;
    }
    reviewFilter = 'nearby';
    elements.reviewFilterNearby.classList.add('sort-active');
    elements.reviewFilterAll.classList.remove('sort-active');
    renderReviewList();
  });

  elements.captureReport.addEventListener('click', () => {
    const captured = captureFrame(elements.reportVideo);
    if (!captured) return;
    state.reportPhoto = captured;
    elements.reportPreview.src = state.reportPhoto;
    elements.reportPreview.classList.remove('hidden');
    elements.reportVideo.classList.add('hidden');
    elements.captureReport.classList.add('hidden');
    elements.retakeReport.classList.remove('hidden');
  });

  elements.retakeReport.addEventListener('click', () => {
    state.reportPhoto = null;
    elements.reportPreview.classList.add('hidden');
    elements.reportVideo.classList.remove('hidden');
    elements.captureReport.classList.remove('hidden');
    elements.retakeReport.classList.add('hidden');
    void attachCamera(elements.reportVideo);
  });

  elements.submitReport.addEventListener('click', () => { void submitReport(); });
  elements.cancelReport.addEventListener('click', closeReportModal);
  elements.closeReport.addEventListener('click', closeReportModal);

  elements.captureClean.addEventListener('click', () => {
    const captured = captureFrame(elements.cleanVideo);
    if (!captured) return;
    state.cleanPhoto = captured;
    elements.cleanPreview.src = state.cleanPhoto;
    elements.cleanPreview.classList.remove('hidden');
    elements.cleanVideo.classList.add('hidden');
    elements.captureClean.classList.add('hidden');
    elements.retakeClean.classList.remove('hidden');
  });

  elements.retakeClean.addEventListener('click', () => {
    state.cleanPhoto = null;
    elements.cleanPreview.classList.add('hidden');
    elements.cleanVideo.classList.remove('hidden');
    elements.captureClean.classList.remove('hidden');
    elements.retakeClean.classList.add('hidden');
    void attachCamera(elements.cleanVideo);
  });

  elements.submitClean.addEventListener('click', () => { void submitClean(); });
  elements.cancelClean.addEventListener('click', closeCleanModal);
  elements.closeClean.addEventListener('click', closeCleanModal);

  elements.closeDetail.addEventListener('click', closeDetailModal);
  elements.markClean.addEventListener('click', () => {
    openCleanModal(elements.markClean.dataset.reportId);
  });
  elements.closePhotos.addEventListener('click', closePhotosModal);



  // Auth bindings
  elements.loginBtn.addEventListener('click', () => auth.showLogin());
  elements.logoutBtn.addEventListener('click', () => auth.logout());
  elements.closeAuth.addEventListener('click', () => auth.hideAuth());
  
  elements.tabLogin.addEventListener('click', () => auth.switchTab('login'));
  elements.tabSignup.addEventListener('click', () => auth.switchTab('signup'));

  elements.submitLogin.addEventListener('click', () => {
    if (elements.loginForm.checkValidity()) {
      auth.login(elements.loginEmail.value, elements.loginPassword.value);
    } else {
      elements.loginForm.reportValidity();
    }
  });

  elements.submitSignup.addEventListener('click', () => {
    if (elements.signupForm.checkValidity()) {
      auth.signup(elements.signupUsername.value, elements.signupEmail.value, elements.signupPassword.value);
    } else {
      elements.signupForm.reportValidity();
    }
  });

  document.querySelectorAll('.tag-button').forEach(btn => {
    btn.addEventListener('click', () => btn.classList.toggle('selected'));
  });
}

async function init() {
  state.db = loadDB();
  initMap();
  bindUI();
  updateAuthUi();
  renderMarkers();
  updateProgress();
  await syncFromApi();
  if (state.apiEnabled) {
    showToast('Connected to CleanMap server.');
    // Try to silently restore session
    await auth.refresh();
  }
  window.addEventListener('beforeunload', stopCamera);
}

document.addEventListener('DOMContentLoaded', init);
