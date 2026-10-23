/**
 * Flight Recorder - Flux UI & ACARS Application Logic
 * Bridges WebView2 and C# State Machine
 */

(function () {
  'use strict';

  // State cache
  const state = {
    currentTab: 'record',
    machineState: 'Start',
    simConnectState: 'NotConnected',
    currentFrame: 0,
    frameCount: 0,
    aircraftTitle: '',
    fileName: '',
    activeProfileName: null,
    activeProfileVendor: null,
    activeProfileVarCount: 0,
    speed: 1,
    repeat: false,
    topmost: false,
    telemetryExpanded: false,
    records: [],
    aircraftPosition: {},
    simState: {},
    savedFlights: [],
    settings: {
      shortcutKeysEnabled: false,
      shortcuts: [],
      savedFlightsFolder: '',
      stayOnTop: false
    },
    recordingHotkeyIndex: null
  };

  // DOM Elements
  const el = {
    // Navigation
    navTabRecord: document.getElementById('navTabRecord'),
    navTabRecords: document.getElementById('navTabRecords'),
    navTabSettings: document.getElementById('navTabSettings'),
    savedFlightsCountBadge: document.getElementById('savedFlightsCountBadge'),
    sidebarConnStatus: document.getElementById('sidebarConnStatus'),
    sidebarConnText: document.getElementById('sidebarConnText'),
    btnOpenSavedFolderSidebar: document.getElementById('btnOpenSavedFolderSidebar'),

    // Views
    viewRecord: document.getElementById('viewRecord'),
    viewRecords: document.getElementById('viewRecords'),
    viewSettings: document.getElementById('viewSettings'),

    // View 1: Record & Controls
    statusBanner: document.getElementById('statusBanner'),
    statusDot: document.getElementById('statusDot'),
    statusText: document.getElementById('statusText'),
    aircraftTitle: document.getElementById('aircraftTitle'),
    aircraftFile: document.getElementById('aircraftFile'),
    saveBadge: document.getElementById('saveBadge'),
    profileBadge: document.getElementById('profileBadge'),
    profileText: document.getElementById('profileText'),
    valProfileDetail: document.getElementById('valProfileDetail'),

    btnRecord: document.getElementById('btnRecord'),
    btnStopRecord: document.getElementById('btnStopRecord'),
    btnReplay: document.getElementById('btnReplay'),
    btnPauseReplay: document.getElementById('btnPauseReplay'),
    btnResumeReplay: document.getElementById('btnResumeReplay'),
    btnStopReplay: document.getElementById('btnStopReplay'),
    btnReplayAI: document.getElementById('btnReplayAI'),

    timelineSlider: document.getElementById('timelineSlider'),
    currentFrameText: document.getElementById('currentFrameText'),
    totalFramesText: document.getElementById('totalFramesText'),
    chartOverlay: document.getElementById('chartOverlay'),
    flightChartCanvas: document.getElementById('flightChart'),

    btnRepeat: document.getElementById('btnRepeat'),
    btnSpeed: document.getElementById('btnSpeed'),
    speedLabel: document.getElementById('speedLabel'),
    speedMenu: document.getElementById('speedMenu'),
    btnTrim: document.getElementById('btnTrim'),
    trimMenu: document.getElementById('trimMenu'),

    btnSave: document.getElementById('btnSave'),
    btnLoad: document.getElementById('btnLoad'),

    telemetryHeader: document.getElementById('telemetryHeader'),
    telemetryBody: document.getElementById('telemetryBody'),
    telemetryChevron: document.getElementById('telemetryChevron'),
    valLat: document.getElementById('valLat'),
    valLon: document.getElementById('valLon'),
    valAlt: document.getElementById('valAlt'),
    valPitch: document.getElementById('valPitch'),
    valBank: document.getElementById('valBank'),
    valHeading: document.getElementById('valHeading'),
    valGear: document.getElementById('valGear'),
    valGround: document.getElementById('valGround'),
    valLandingRate: document.getElementById('valLandingRate'),
    valThrottle: document.getElementById('valThrottle'),
    valFlaps: document.getElementById('valFlaps'),

    // View 2: Records List
    inputSearchRecords: document.getElementById('inputSearchRecords'),
    statTotalFlights: document.getElementById('statTotalFlights'),
    statTotalTime: document.getElementById('statTotalTime'),
    recordsListContainer: document.getElementById('recordsListContainer'),
    btnRefreshSaved: document.getElementById('btnRefreshSaved'),
    btnImportFlight: document.getElementById('btnImportFlight'),
    btnOpenFolderRecords: document.getElementById('btnOpenFolderRecords'),

    // View 3: Settings
    btnSaveSettings: document.getElementById('btnSaveSettings'),
    btnRestoreDefaults: document.getElementById('btnRestoreDefaults'),
    toggleHotkeysEnabled: document.getElementById('toggleHotkeysEnabled'),
    hotkeysContainer: document.getElementById('hotkeysContainer'),
    textSavedFolderPath: document.getElementById('textSavedFolderPath'),
    btnOpenSettingsFolder: document.getElementById('btnOpenSettingsFolder'),
    toggleStayOnTopSettings: document.getElementById('toggleStayOnTopSettings'),

    // Toasts & Updates
    toast: document.getElementById('toast'),
    updateBanner: document.getElementById('updateBanner'),
    updateBannerText: document.getElementById('updateBannerText'),
    updateProgressFill: document.getElementById('updateProgressFill')
  };

  // Chart instance
  let chart = null;
  if (el.flightChartCanvas && typeof FlightChart !== 'undefined') {
    chart = new FlightChart(el.flightChartCanvas, (frame) => {
      seekToFrame(frame);
    });
  }

  // Communication Helper
  function sendToHost(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(JSON.stringify({ action, ...data }));
    }
  }

  // Toast Notification
  let toastTimer = null;
  function showToast(message, duration = 3000) {
    if (!el.toast) return;
    el.toast.textContent = message;
    el.toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      el.toast.classList.remove('show');
    }, duration);
  }

  // Tab Navigation Switching
  function switchTab(tabName) {
    state.currentTab = tabName;

    // Update nav items
    document.querySelectorAll('.nav-item').forEach((item) => {
      item.classList.toggle('active', item.dataset.tab === tabName);
    });

    // Update tab sections
    if (el.viewRecord) el.viewRecord.classList.toggle('active', tabName === 'record');
    if (el.viewRecords) el.viewRecords.classList.toggle('active', tabName === 'records');
    if (el.viewSettings) el.viewSettings.classList.toggle('active', tabName === 'settings');

    if (tabName === 'records') {
      sendToHost('getSavedFlights');
    } else if (tabName === 'settings') {
      sendToHost('getSettings');
    } else if (tabName === 'record') {
      if (chart) chart.resize();
    }
  }

  // Formatting Helpers
  function formatDuration(seconds) {
    if (!seconds || seconds <= 0) return '0s';
    const m = Math.floor(seconds / 60);
    const s = Math.floor(seconds % 60);
    if (m === 0) return `${s}s`;
    return `${m}m ${s}s`;
  }

  function formatBytes(bytes) {
    if (!bytes || bytes === 0) return '0 KB';
    const k = 1024;
    if (bytes < k * k) return `${(bytes / k).toFixed(1)} KB`;
    return `${(bytes / (k * k)).toFixed(1)} MB`;
  }

  function formatDate(dateStr) {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hours = String(d.getHours()).padStart(2, '0');
    const mins = String(d.getMinutes()).padStart(2, '0');
    return `${day}/${month}/${year} ${hours}:${mins}`;
  }

  // Render Saved Flights (View 2)
  function renderSavedFlights() {
    if (!el.recordsListContainer) return;

    const query = (el.inputSearchRecords?.value || '').trim().toLowerCase();
    const filtered = state.savedFlights.filter((flight) => {
      if (!query) return true;
      const title = (flight.aircraftTitle || '').toLowerCase();
      const name = (flight.fileName || '').toLowerCase();
      return title.includes(query) || name.includes(query);
    });

    // Update stats
    if (el.statTotalFlights) el.statTotalFlights.textContent = state.savedFlights.length;
    if (el.savedFlightsCountBadge) el.savedFlightsCountBadge.textContent = state.savedFlights.length;

    const totalSeconds = state.savedFlights.reduce((acc, f) => acc + (f.durationSeconds || 0), 0);
    const totalMinutes = Math.round(totalSeconds / 60);
    if (el.statTotalTime) el.statTotalTime.textContent = `${totalMinutes} min`;

    if (filtered.length === 0) {
      el.recordsListContainer.innerHTML = `
        <div class="empty-state">
          <div class="empty-state-icon">✈️</div>
          <div class="empty-state-title">Nenhum voo salvo encontrado</div>
          <div class="empty-state-desc">
            ${query ? 'Nenhum resultado corresponde à sua pesquisa.' : 'Grave um voo no simulador e clique no botão Save para salvá-lo aqui.'}
          </div>
        </div>
      `;
      return;
    }

    el.recordsListContainer.innerHTML = '';
    filtered.forEach((flight) => {
      const card = document.createElement('div');
      card.className = 'flight-card';

      const durText = formatDuration(flight.durationSeconds);
      const sizeText = formatBytes(flight.fileSizeBytes);
      const dateText = formatDate(flight.createdAt);
      const frameText = `${(flight.frameCount || 0).toLocaleString()} frames`;

      card.innerHTML = `
        <div class="flight-card-header">
          <div class="flight-card-title-group">
            <span class="flight-card-plane" title="${flight.aircraftTitle || 'Flight'}">${flight.aircraftTitle || 'Flight'}</span>
            <span class="flight-card-date">${dateText}</span>
          </div>
        </div>
        <div class="flight-badges-row">
          <span class="flight-badge duration">⏱ ${durText}</span>
          <span class="flight-badge">🎞 ${frameText}</span>
          <span class="flight-badge">💾 ${sizeText}</span>
          ${flight.profileId ? `<span class="flight-badge" style="color: #38bdf8; border-color: rgba(56,189,248,0.3);">⚡ ${flight.profileId}</span>` : ''}
        </div>
        <div class="flight-card-footer">
          <span class="flight-filename" title="${flight.fileName}">${flight.fileName}</span>
          <div class="flight-card-actions">
            <button class="btn btn-accent btn-sm btn-load-flight" title="Carregar este voo para Replay">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
                <polygon points="5 3 19 12 5 21 5 3"/>
              </svg>
              Carregar
            </button>
            <button class="btn btn-danger btn-sm btn-delete-flight" title="Excluir gravação">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
              </svg>
            </button>
          </div>
        </div>
      `;

      // Load click
      card.querySelector('.btn-load-flight').addEventListener('click', () => {
        sendToHost('loadSavedFlight', { path: flight.filePath });
      });

      // Delete click
      card.querySelector('.btn-delete-flight').addEventListener('click', () => {
        if (confirm(`Deseja realmente excluir o voo "${flight.aircraftTitle || flight.fileName}"?`)) {
          sendToHost('deleteSavedFlight', { path: flight.filePath });
        }
      });

      el.recordsListContainer.appendChild(card);
    });
  }

  // Render Hotkeys Settings (View 3)
  function renderSettings() {
    if (!el.hotkeysContainer) return;

    if (el.toggleHotkeysEnabled) {
      el.toggleHotkeysEnabled.checked = state.settings.shortcutKeysEnabled;
    }
    if (el.toggleStayOnTopSettings) {
      el.toggleStayOnTopSettings.checked = state.settings.stayOnTop;
    }
    if (el.textSavedFolderPath && state.settings.savedFlightsFolder) {
      el.textSavedFolderPath.textContent = state.settings.savedFlightsFolder;
    }

    el.hotkeysContainer.innerHTML = '';
    (state.settings.shortcuts || []).forEach((item, index) => {
      const row = document.createElement('div');
      row.className = 'hotkey-row';

      const isRecording = state.recordingHotkeyIndex === index;

      row.innerHTML = `
        <span class="hotkey-name">${item.name || item.id}</span>
        <div class="hotkey-modifiers">
          <label class="mod-check-label">
            <input type="checkbox" class="chk-ctrl" ${item.ctrl ? 'checked' : ''}>
            <span>Ctrl</span>
          </label>
          <label class="mod-check-label">
            <input type="checkbox" class="chk-alt" ${item.alt ? 'checked' : ''}>
            <span>Alt</span>
          </label>
          <label class="mod-check-label">
            <input type="checkbox" class="chk-shift" ${item.shift ? 'checked' : ''}>
            <span>Shift</span>
          </label>
          <button class="hotkey-key-btn ${isRecording ? 'recording' : ''}" title="Clique para alterar a tecla">
            ${isRecording ? 'Pressione tecla...' : (item.key || 'Nenhum')}
          </button>
          <span class="hotkey-preview-badge">${formatShortcutPreview(item)}</span>
        </div>
      `;

      // Checkbox events
      row.querySelector('.chk-ctrl').addEventListener('change', (e) => {
        item.ctrl = e.target.checked;
        row.querySelector('.hotkey-preview-badge').textContent = formatShortcutPreview(item);
      });
      row.querySelector('.chk-alt').addEventListener('change', (e) => {
        item.alt = e.target.checked;
        row.querySelector('.hotkey-preview-badge').textContent = formatShortcutPreview(item);
      });
      row.querySelector('.chk-shift').addEventListener('change', (e) => {
        item.shift = e.target.checked;
        row.querySelector('.hotkey-preview-badge').textContent = formatShortcutPreview(item);
      });

      // Key button capture
      const keyBtn = row.querySelector('.hotkey-key-btn');
      keyBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        if (state.recordingHotkeyIndex === index) {
          state.recordingHotkeyIndex = null;
        } else {
          state.recordingHotkeyIndex = index;
        }
        renderSettings();
      });

      el.hotkeysContainer.appendChild(row);
    });
  }

  function formatShortcutPreview(item) {
    const parts = [];
    if (item.ctrl) parts.push('Ctrl');
    if (item.alt) parts.push('Alt');
    if (item.shift) parts.push('Shift');
    if (item.key) parts.push(item.key);
    return parts.length > 0 ? parts.join(' + ') : 'Nenhum';
  }

  // Keydown listener for capturing hotkeys in settings
  window.addEventListener('keydown', (e) => {
    if (state.recordingHotkeyIndex === null) return;
    e.preventDefault();
    e.stopPropagation();

    const ignoredKeys = ['Control', 'Alt', 'Shift', 'Meta', 'CapsLock', 'Tab'];
    if (ignoredKeys.includes(e.key)) return;

    const item = state.settings.shortcuts[state.recordingHotkeyIndex];
    if (item) {
      let keyName = e.key;
      if (keyName.length === 1) keyName = keyName.toUpperCase();
      if (keyName === ' ') keyName = 'Space';
      if (keyName === 'ArrowUp') keyName = 'Up';
      if (keyName === 'ArrowDown') keyName = 'Down';
      if (keyName === 'ArrowLeft') keyName = 'Left';
      if (keyName === 'ArrowRight') keyName = 'Right';

      item.key = keyName;
      item.virtualKey = e.keyCode || 0;
      state.recordingHotkeyIndex = null;
      renderSettings();
      showToast(`Tecla definida: ${keyName}`);
    }
  });

  // Host Message Dispatcher
  function handleHostMessage(msg) {
    if (!msg || !msg.type) return;

    switch (msg.type) {
      case 'stateUpdate':
        Object.assign(state, msg.payload);
        render();
        break;

      case 'telemetryUpdate':
        if (msg.payload && msg.payload.position) {
          updateTelemetry(msg.payload.position);
        }
        break;

      case 'recordsUpdate':
        state.records = msg.payload?.records || [];
        if (chart) chart.setData(state.records);
        updateTimelineUI();
        break;

      case 'savedFlightsList':
        state.savedFlights = msg.payload || [];
        renderSavedFlights();
        break;

      case 'flightLoaded':
        showToast(`Voo "${msg.payload?.aircraftTitle || msg.payload?.fileName}" carregado com sucesso!`);
        switchTab('record');
        break;

      case 'settingsData':
        if (msg.payload) {
          state.settings = msg.payload;
          renderSettings();
        }
        break;

      case 'toast':
        showToast(msg.payload || '');
        break;

      case 'updateStatus':
        handleUpdateBanner(msg.payload);
        break;

      case 'updateProgress':
        if (el.updateProgressFill && msg.payload?.progress !== undefined) {
          el.updateProgressFill.style.width = `${msg.payload.progress}%`;
        }
        break;
    }
  }

  function handleUpdateBanner(payload) {
    if (!el.updateBanner) return;
    if (payload.status === 'downloading') {
      el.updateBanner.classList.remove('hidden');
      if (el.updateBannerText) {
        el.updateBannerText.textContent = `Nova versão ${payload.version} encontrada. Baixando instalador...`;
      }
    } else if (payload.status === 'ready') {
      el.updateBanner.classList.remove('hidden');
      if (el.updateBannerText) {
        el.updateBannerText.textContent = `Atualização ${payload.version} pronta! Reiniciando...`;
      }
      if (el.updateProgressFill) {
        el.updateProgressFill.style.width = '100%';
      }
    } else {
      el.updateBanner.classList.add('hidden');
    }
  }

  // Render View 1 (Record & Control Panel)
  function render() {
    // SimConnect Status
    const isConn = state.simConnectState === 'Connected';
    if (el.statusBanner) {
      el.statusBanner.className = `status-banner ${isConn ? 'connected' : 'disconnected'}`;
    }
    if (el.statusText) {
      el.statusText.textContent = isConn
        ? 'Connected to Microsoft Flight Simulator'
        : 'SimConnect Disconnected — Please start MSFS';
    }

    // Sidebar status pill
    if (el.sidebarConnStatus) {
      el.sidebarConnStatus.className = `conn-pill ${isConn ? 'connected' : 'disconnected'}`;
    }
    if (el.sidebarConnText) {
      el.sidebarConnText.textContent = isConn ? 'MSFS Connected' : 'MSFS Disconnected';
    }

    // Aircraft Card
    if (el.aircraftTitle) {
      el.aircraftTitle.textContent = state.aircraftTitle || 'No aircraft detected';
    }
    if (el.aircraftFile) {
      el.aircraftFile.textContent = state.fileName ? state.fileName : (state.frameCount > 0 ? `${state.frameCount} frames gravados` : 'No active recording');
    }

    // HubHop Profile badge
    if (el.profileBadge && el.profileText) {
      if (state.activeProfileName) {
        el.profileBadge.className = 'profile-badge active';
        el.profileText.textContent = `Perfil ${state.activeProfileName}`;
        if (el.valProfileDetail) {
          el.valProfileDetail.textContent = `${state.activeProfileName} (${state.activeProfileVarCount} L-Vars)`;
        }
      } else {
        el.profileBadge.className = 'profile-badge standard';
        el.profileText.textContent = 'Perfil Standard';
        if (el.valProfileDetail) {
          el.valProfileDetail.textContent = 'Standard SimConnect';
        }
      }
    }

    // State machine button states
    updateButtonStates();
    updateTimelineUI();
  }

  function updateButtonStates() {
    const s = state.machineState;
    const isRecording = s === 'Recording';
    const isReplaying = s === 'ReplayingSaved' || s === 'ReplayingUnsaved';
    const isPaused = s === 'PausingSaved' || s === 'PausingUnsaved';
    const isUnsaved = s.includes('Unsaved');
    const hasData = state.frameCount > 0;

    // Toggle Record / Stop Recording buttons
    if (el.btnRecord && el.btnStopRecord) {
      el.btnRecord.style.display = isRecording ? 'none' : 'inline-flex';
      el.btnStopRecord.style.display = isRecording ? 'inline-flex' : 'none';
      el.btnRecord.disabled = !state.simConnectState.includes('Connected') && s !== 'IdleEmpty';
    }

    // Replay buttons
    if (el.btnReplay) el.btnReplay.disabled = !hasData || isRecording || isReplaying;
    if (el.btnReplayAI) el.btnReplayAI.disabled = !hasData || isRecording || isReplaying;
    if (el.btnPauseReplay) el.btnPauseReplay.disabled = !isReplaying;
    if (el.btnResumeReplay) el.btnResumeReplay.disabled = !isPaused;
    if (el.btnStopReplay) el.btnStopReplay.disabled = !isReplaying && !isPaused;

    // Persistence buttons
    if (el.btnSave) el.btnSave.disabled = !isUnsaved && !hasData;
    if (el.btnLoad) el.btnLoad.disabled = isRecording;

    // Saved badge
    if (el.saveBadge) {
      el.saveBadge.style.display = (s === 'IdleSaved' || s === 'DisconnectedSaved') ? 'inline-flex' : 'none';
    }
  }

  function updateTimelineUI() {
    if (el.currentFrameText) el.currentFrameText.textContent = state.currentFrame || 0;
    if (el.totalFramesText) el.totalFramesText.textContent = state.frameCount || 0;

    if (el.timelineSlider) {
      el.timelineSlider.max = Math.max(0, state.frameCount - 1);
      el.timelineSlider.value = state.currentFrame || 0;
      el.timelineSlider.disabled = state.frameCount <= 1;
    }

    if (el.chartOverlay) {
      el.chartOverlay.style.display = (state.frameCount > 0) ? 'none' : 'flex';
    }

    if (chart) {
      chart.setCurrentFrame(state.currentFrame || 0);
    }
  }

  function seekToFrame(frame) {
    state.currentFrame = frame;
    updateTimelineUI();
    sendToHost('seek', { frame });
  }

  function updateTelemetry(pos) {
    if (!pos) return;
    if (el.valLat) el.valLat.textContent = pos.latitude ? pos.latitude.toFixed(5) : '--';
    if (el.valLon) el.valLon.textContent = pos.longitude ? pos.longitude.toFixed(5) : '--';
    if (el.valAlt) el.valAlt.textContent = pos.altitude ? `${Math.round(pos.altitude)} ft` : '--';
    if (el.valPitch && el.valBank) {
      el.valPitch.textContent = pos.pitch ? `${pos.pitch.toFixed(1)}°` : '--';
      el.valBank.textContent = pos.bank ? `${pos.bank.toFixed(1)}°` : '--';
    }
    if (el.valHeading) {
      el.valHeading.textContent = pos.trueHeading ? `${Math.round(pos.trueHeading)}°` : '--';
    }
    if (el.valGear) el.valGear.textContent = pos.gearHandlePosition ? 'DOWN' : 'UP';
    if (el.valGround) {
      el.valGround.textContent = pos.isOnGround ? 'GND' : 'AIR';
      el.valGround.style.color = pos.isOnGround ? '#f59e0b' : '#34d399';
    }
    if (el.valLandingRate) {
      if (pos.touchdownNormalVelocity) {
        const fpm = Math.round(pos.touchdownNormalVelocity * 60);
        el.valLandingRate.textContent = `${fpm} fpm`;
      } else {
        el.valLandingRate.textContent = '--';
      }
    }
    if (el.valThrottle) {
      el.valThrottle.textContent = (pos.throttleLeverPosition1 !== undefined)
        ? `${Math.round(pos.throttleLeverPosition1)}%` : '--';
    }
    if (el.valFlaps) {
      el.valFlaps.textContent = (pos.flapsHandleIndex !== undefined)
        ? `${pos.flapsHandleIndex}` : '--';
    }
  }

  // Event Listeners Initialization
  function initListeners() {
    // Navigation tabs
    el.navTabRecord?.addEventListener('click', () => switchTab('record'));
    el.navTabRecords?.addEventListener('click', () => switchTab('records'));
    el.navTabSettings?.addEventListener('click', () => switchTab('settings'));

    // Primary Actions
    el.btnRecord?.addEventListener('click', () => sendToHost('record'));
    el.btnStopRecord?.addEventListener('click', () => sendToHost('stop'));
    el.btnReplay?.addEventListener('click', () => sendToHost('replay'));
    el.btnPauseReplay?.addEventListener('click', () => sendToHost('pause'));
    el.btnResumeReplay?.addEventListener('click', () => sendToHost('resume'));
    el.btnStopReplay?.addEventListener('click', () => sendToHost('stopReplay'));
    el.btnReplayAI?.addEventListener('click', () => sendToHost('replayAI'));

    // Persistence: Save directly, Load switches to Records view
    el.btnSave?.addEventListener('click', () => sendToHost('save'));
    el.btnLoad?.addEventListener('click', () => switchTab('records'));

    // Timeline Slider
    el.timelineSlider?.addEventListener('input', (e) => {
      seekToFrame(parseInt(e.target.value, 10));
    });

    // Repeat Toggle
    el.btnRepeat?.addEventListener('click', () => {
      state.repeat = !state.repeat;
      el.btnRepeat.classList.toggle('active', state.repeat);
      sendToHost('setRepeat', { repeat: state.repeat });
      showToast(state.repeat ? 'Loop ativado' : 'Loop desativado');
    });

    // Speed Menu
    el.btnSpeed?.addEventListener('click', (e) => {
      e.stopPropagation();
      closeAllMenus();
      el.speedMenu?.classList.toggle('show');
    });

    document.querySelectorAll('[data-speed]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        const val = parseFloat(e.currentTarget.dataset.speed);
        state.speed = val;
        if (el.speedLabel) el.speedLabel.textContent = `x${val}`;
        sendToHost('setSpeed', { rate: val });
        el.speedMenu?.classList.remove('show');
        showToast(`Velocidade de Replay: x${val}`);
      });
    });

    // Trim Menu
    el.btnTrim?.addEventListener('click', (e) => {
      e.stopPropagation();
      closeAllMenus();
      el.trimMenu?.classList.toggle('show');
    });

    document.getElementById('btnTrimStart')?.addEventListener('click', () => {
      sendToHost('trimStart');
      el.trimMenu?.classList.remove('show');
      showToast('Início do voo cortado até este frame');
    });

    document.getElementById('btnTrimEnd')?.addEventListener('click', () => {
      sendToHost('trimEnd');
      el.trimMenu?.classList.remove('show');
      showToast('Fim do voo cortado até este frame');
    });

    // Telemetry Accordion
    el.telemetryHeader?.addEventListener('click', () => {
      state.telemetryExpanded = !state.telemetryExpanded;
      el.telemetryBody?.classList.toggle('collapsed', !state.telemetryExpanded);
      if (el.telemetryChevron) {
        el.telemetryChevron.style.transform = state.telemetryExpanded ? 'rotate(180deg)' : 'rotate(0deg)';
      }
    });

    // Copyable metrics
    document.querySelectorAll('.copyable').forEach((cell) => {
      cell.addEventListener('click', () => {
        const val = cell.querySelector('.metric-value')?.textContent;
        if (val && val !== '--') {
          navigator.clipboard.writeText(val);
          showToast(`Copiado: ${val}`);
        }
      });
    });

    // Records Actions
    el.inputSearchRecords?.addEventListener('input', () => renderSavedFlights());
    el.btnRefreshSaved?.addEventListener('click', () => {
      sendToHost('getSavedFlights');
      showToast('Lista de voos atualizada');
    });
    el.btnImportFlight?.addEventListener('click', () => sendToHost('importSavedFlight'));
    el.btnOpenFolderRecords?.addEventListener('click', () => sendToHost('openSavedFlightsFolder'));
    el.btnOpenSavedFolderSidebar?.addEventListener('click', () => sendToHost('openSavedFlightsFolder'));

    // Settings Actions
    el.toggleHotkeysEnabled?.addEventListener('change', (e) => {
      state.settings.shortcutKeysEnabled = e.target.checked;
    });
    el.toggleStayOnTopSettings?.addEventListener('change', (e) => {
      state.settings.stayOnTop = e.target.checked;
      sendToHost('setTopmost', { topmost: e.target.checked });
      showToast(e.target.checked ? 'Stay on Top: Ativado' : 'Stay on Top: Desativado');
    });
    el.btnOpenSettingsFolder?.addEventListener('click', () => sendToHost('openSavedFlightsFolder'));

    el.btnSaveSettings?.addEventListener('click', () => {
      sendToHost('saveShortcutSettings', {
        enabled: state.settings.shortcutKeysEnabled,
        shortcuts: state.settings.shortcuts
      });
    });

    el.btnRestoreDefaults?.addEventListener('click', () => {
      if (confirm('Deseja restaurar os atalhos para a configuração padrão?')) {
        sendToHost('restoreDefaultShortcuts');
      }
    });

    // Global click closes dropdowns
    document.addEventListener('click', () => closeAllMenus());

    function closeAllMenus() {
      el.speedMenu?.classList.remove('show');
      el.trimMenu?.classList.remove('show');
    }
  }

  // WebView2 Message Bridge Registration
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      handleHostMessage(event.data);
    });
  }

  // Initial startup
  initListeners();
  render();

  // Signal host that UI is ready
  sendToHost('uiReady');
})();
