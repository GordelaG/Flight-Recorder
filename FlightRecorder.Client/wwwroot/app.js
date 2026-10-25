/**
 * Flight Recorder - Flux UI & ACARS Application Logic
 * Bridges WebView2 and C# State Machine
 */

(function () {
  'use strict';

  // Default Global Hotkeys matching C# ShortcutKeyLogic
  const DEFAULT_SHORTCUTS = [
    {
      id: 'Record',
      name: 'Gravar Voo (Record)',
      desc: 'Inicia a gravação das posições do voo',
      ctrl: true,
      alt: true,
      shift: true,
      key: 'Home',
      virtualKey: 0x24
    },
    {
      id: 'StopRecording',
      name: 'Parar Gravação (Stop)',
      desc: 'Interrompe a gravação atual',
      ctrl: true,
      alt: true,
      shift: true,
      key: 'End',
      virtualKey: 0x23
    },
    {
      id: 'Replay',
      name: 'Replay do Voo (Play)',
      desc: 'Inicia a reprodução do voo gravado',
      ctrl: true,
      alt: true,
      shift: true,
      key: 'R',
      virtualKey: 82
    },
    {
      id: 'Pause',
      name: 'Pausar Replay (Pause)',
      desc: 'Congela a reprodução do voo no simulador',
      ctrl: true,
      alt: true,
      shift: true,
      key: ',',
      virtualKey: 0xBC
    },
    {
      id: 'Resume',
      name: 'Continuar Replay (Resume)',
      desc: 'Retoma a reprodução pausada',
      ctrl: true,
      alt: true,
      shift: true,
      key: '.',
      virtualKey: 0xBE
    },
    {
      id: 'StopReplay',
      name: 'Parar Replay (Stop Replay)',
      desc: 'Encerra o replay e devolve os controles do simulador',
      ctrl: true,
      alt: true,
      shift: true,
      key: 'S',
      virtualKey: 83
    },
    {
      id: 'SaveToDisk',
      name: 'Salvar Voo Rápido (Quick Save)',
      desc: 'Salva o voo gravado diretamente na pasta SavedFlights',
      ctrl: true,
      alt: true,
      shift: true,
      key: 'C',
      virtualKey: 67
    },
    {
      id: 'ToggleReplayHud',
      name: 'Barra de Replay (Mini Player)',
      desc: 'Abre ou fecha o popup flutuante da barra de controle de replay',
      ctrl: true,
      alt: true,
      shift: true,
      key: 'H',
      virtualKey: 72
    }
  ];

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
      shortcuts: JSON.parse(JSON.stringify(DEFAULT_SHORTCUTS)),
      savedFlightsFolder: 'C:\\Program Files\\Flight Recorder\\SavedFlights',
      stayOnTop: false
    },
    recordingHotkeyIndex: null
  };

  let tempShortcuts = [];
  let liveModifiers = { ctrl: false, alt: false, shift: false };

  // DOM Elements
  const el = {
    // Navigation
    navTabRecord: document.getElementById('navTabRecord'),
    navTabRecords: document.getElementById('navTabRecords'),
    navTabTelemetry: document.getElementById('navTabTelemetry'),
    navTabSettings: document.getElementById('navTabSettings'),
    savedFlightsCountBadge: document.getElementById('savedFlightsCountBadge'),

    // Views
    viewRecord: document.getElementById('viewRecord'),
    viewRecords: document.getElementById('viewRecords'),
    viewTelemetry: document.getElementById('viewTelemetry'),
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

    valLat: document.getElementById('valLat'),
    valLon: document.getElementById('valLon'),
    valAlt: document.getElementById('valAlt'),
    valPitch: document.getElementById('valPitch'),
    valBank: document.getElementById('valBank'),
    valHeading: document.getElementById('valHeading'),
    valGear: document.getElementById('valGear'),
    valLandingRate: document.getElementById('valLandingRate'),
    valThrottle: document.getElementById('valThrottle'),
    valFlaps: document.getElementById('valFlaps'),
    btnCopyCoords: document.getElementById('btnCopyCoords'),
    telemetryConnPill: document.getElementById('telemetryConnPill'),
    telemetryConnText: document.getElementById('telemetryConnText'),
    telemAircraftTitle: document.getElementById('telemAircraftTitle'),
    telemAircraftSub: document.getElementById('telemAircraftSub'),
    telemProfileBadge: document.getElementById('telemProfileBadge'),
    telemProfileText: document.getElementById('telemProfileText'),
    telemContactBadge: document.getElementById('telemContactBadge'),
    barThrottle: document.getElementById('barThrottle'),
    barFlaps: document.getElementById('barFlaps'),

    // View 2: Records List
    inputSearchRecords: document.getElementById('inputSearchRecords'),
    statTotalFlights: document.getElementById('statTotalFlights'),
    statTotalTime: document.getElementById('statTotalTime'),
    recordsListContainer: document.getElementById('recordsListContainer'),
    btnRefreshSaved: document.getElementById('btnRefreshSaved'),
    btnImportFlight: document.getElementById('btnImportFlight'),
    btnOpenFolderRecords: document.getElementById('btnOpenFolderRecords'),

    // View 3: Settings
    toggleHotkeysEnabled: document.getElementById('toggleHotkeysEnabled'),
    hotkeysStatusBadge: document.getElementById('hotkeysStatusBadge'),
    hotkeysSummaryBadge: document.getElementById('hotkeysSummaryBadge'),
    btnOpenHotkeysModal: document.getElementById('btnOpenHotkeysModal'),
    textSavedFolderPath: document.getElementById('textSavedFolderPath'),
    btnOpenSettingsFolder: document.getElementById('btnOpenSettingsFolder'),
    toggleStayOnTopSettings: document.getElementById('toggleStayOnTopSettings'),
    toggleHudAutoHideSettings: document.getElementById('toggleHudAutoHideSettings'),
    hudStyleSegmentedGroup: document.getElementById('hudStyleSegmentedGroup'),

    // Modal: Hotkeys Configuration (MSFS 2024 Style)
    hotkeysModal: document.getElementById('hotkeysModal'),
    modalHotkeysList: document.getElementById('modalHotkeysList'),
    btnCloseHotkeysModal: document.getElementById('btnCloseHotkeysModal'),
    btnCancelHotkeysModal: document.getElementById('btnCancelHotkeysModal'),
    btnSaveHotkeysModal: document.getElementById('btnSaveHotkeysModal'),
    btnRestoreDefaultsModal: document.getElementById('btnRestoreDefaultsModal'),

    // Toasts & Updates
    toast: document.getElementById('toast'),
    updateBanner: document.getElementById('updateBanner'),
    updateBannerText: document.getElementById('updateBannerText'),
    updateProgressFill: document.getElementById('updateProgressFill'),

    // Floating Replay HUD Launcher
    btnOpenReplayHud: document.getElementById('btnOpenReplayHud')
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
      window.chrome.webview.postMessage({ action, ...data });
    }
  }

  // Theme Management (Flux UI Dark / Light / System)
  function initTheme() {
    const savedTheme = localStorage.getItem('flightrecorder.theme') || 'dark';
    applyTheme(savedTheme, false);

    // Watch system OS color scheme changes if user chose 'system'
    if (window.matchMedia) {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const current = localStorage.getItem('flightrecorder.theme') || 'dark';
        if (current === 'system') {
          applyTheme('system', false);
        }
      });
    }
  }

  function applyTheme(theme, save = true) {
    if (save) {
      localStorage.setItem('flightrecorder.theme', theme);
    }

    let effectiveTheme = theme;
    if (theme === 'system') {
      const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
      effectiveTheme = prefersDark ? 'dark' : 'light';
    }

    const root = document.documentElement;
    if (effectiveTheme === 'light') {
      root.classList.remove('dark');
      root.classList.add('light');
    } else {
      root.classList.remove('light');
      root.classList.add('dark');
    }

    // Update segmented theme buttons in Settings
    document.querySelectorAll('.theme-seg-btn').forEach((btn) => {
      btn.classList.toggle('active', btn.dataset.theme === theme);
    });

    // Re-render chart with appropriate grid and colors
    if (chart) {
      chart.render();
    }

    // Send theme change to C# host to sync WPF window background
    sendToHost('themeChanged', { theme, effectiveTheme });
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

  // Custom Confirmation Dialog (Replaces native browser confirm)
  function showConfirmDialog({
    title = 'Confirmar Ação',
    message = 'Tem certeza de que deseja continuar?',
    confirmText = 'Confirmar',
    cancelText = 'Cancelar',
    variant = 'warning'
  } = {}) {
    return new Promise((resolve) => {
      const modal = document.getElementById('confirmModal');
      const titleEl = document.getElementById('confirmModalTitle');
      const msgEl = document.getElementById('confirmModalMessage');
      const btnOk = document.getElementById('btnConfirmOk');
      const btnCancel = document.getElementById('btnConfirmCancel');
      const iconWrapper = document.getElementById('confirmModalIconWrapper');

      if (!modal) {
        resolve(window.confirm(message));
        return;
      }

      if (titleEl) titleEl.textContent = title;
      if (msgEl) msgEl.textContent = message;
      if (btnCancel) btnCancel.textContent = cancelText;

      if (btnOk) {
        btnOk.textContent = confirmText;
        if (variant === 'danger') {
          btnOk.className = 'btn btn-record btn-sm';
        } else if (variant === 'warning') {
          btnOk.className = 'btn btn-warning btn-sm';
        } else {
          btnOk.className = 'btn btn-accent btn-sm';
        }
      }

      if (iconWrapper) {
        iconWrapper.className = `confirm-modal-icon-wrapper variant-${variant}`;
        if (variant === 'danger') {
          iconWrapper.innerHTML = `
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="15" y1="9" x2="9" y2="15"/>
              <line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
          `;
        } else if (variant === 'info') {
          iconWrapper.innerHTML = `
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="12" y1="16" x2="12" y2="12"/>
              <line x1="12" y1="8" x2="12.01" y2="8"/>
            </svg>
          `;
        } else {
          // Warning / Restore
          iconWrapper.innerHTML = `
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
              <line x1="12" y1="9" x2="12" y2="13"/>
              <line x1="12" y1="17" x2="12.01" y2="17"/>
            </svg>
          `;
        }
      }

      modal.classList.remove('hidden');

      const cleanup = () => {
        btnOk?.removeEventListener('click', onOk);
        btnCancel?.removeEventListener('click', onCancel);
        modal.removeEventListener('click', onOverlay);
        window.removeEventListener('keydown', onKeyDown);
        modal.classList.add('hidden');
      };

      const onOk = () => {
        cleanup();
        resolve(true);
      };

      const onCancel = () => {
        cleanup();
        resolve(false);
      };

      const onOverlay = (e) => {
        if (e.target === modal) {
          cleanup();
          resolve(false);
        }
      };

      const onKeyDown = (e) => {
        if (e.key === 'Escape') {
          e.stopPropagation();
          cleanup();
          resolve(false);
        } else if (e.key === 'Enter') {
          e.stopPropagation();
          cleanup();
          resolve(true);
        }
      };

      btnOk?.addEventListener('click', onOk);
      btnCancel?.addEventListener('click', onCancel);
      modal.addEventListener('click', onOverlay);
      window.addEventListener('keydown', onKeyDown);

      setTimeout(() => btnOk?.focus(), 50);
    });
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
    if (el.viewTelemetry) el.viewTelemetry.classList.toggle('active', tabName === 'telemetry');
    if (el.viewSettings) el.viewSettings.classList.toggle('active', tabName === 'settings');

    if (tabName === 'records') {
      sendToHost('getSavedFlights');
    } else if (tabName === 'settings') {
      renderSettings();
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

  function formatTimeFromFrames(currentFrame, totalFrames) {
    const totalSeconds = (currentFrame || 0) / 60;
    const mins = Math.floor(totalSeconds / 60);
    const secs = Math.floor(totalSeconds % 60);
    const hundredths = Math.floor((totalSeconds % 1) * 100);
    const mStr = String(mins).padStart(2, '0');
    const sStr = String(secs).padStart(2, '0');
    const hStr = String(hundredths).padStart(2, '0');
    return `${mStr}:${sStr}.${hStr}`;
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

  // Render Settings View (Compact)
  function renderSettings() {
    const isEnabled = !!state.settings.shortcutKeysEnabled;
    if (el.toggleHotkeysEnabled) {
      el.toggleHotkeysEnabled.checked = isEnabled;
    }
    if (el.hotkeysStatusBadge) {
      el.hotkeysStatusBadge.textContent = isEnabled ? 'Ativado' : 'Desativado';
      el.hotkeysStatusBadge.className = `status-pill ${isEnabled ? 'status-pill-green' : 'status-pill-amber'}`;
    }
    if (el.hotkeysSummaryBadge) {
      const activeCount = (state.settings.shortcuts || []).filter(s => s && s.key).length;
      el.hotkeysSummaryBadge.textContent = `${activeCount} atalhos configurados`;
    }
    if (el.toggleStayOnTopSettings) {
      el.toggleStayOnTopSettings.checked = !!state.settings.stayOnTop;
    }
    if (el.toggleHudAutoHideSettings) {
      el.toggleHudAutoHideSettings.checked = !!state.settings.hudAutoHide;
    }
    if (el.hudStyleSegmentedGroup) {
      const isTranslucent = !!state.settings.hudTranslucent;
      el.hudStyleSegmentedGroup.querySelectorAll('.theme-seg-btn').forEach(btn => {
        const isTrBtn = btn.getAttribute('data-hud-style') === 'translucent';
        btn.classList.toggle('active', isTranslucent ? isTrBtn : !isTrBtn);
      });
    }
    if (el.textSavedFolderPath && state.settings.savedFlightsFolder) {
      el.textSavedFolderPath.textContent = state.settings.savedFlightsFolder;
    }
  }

  // Render Hotkeys Configuration Modal (MSFS 2024 Style)
  function renderModalHotkeys() {
    if (!el.modalHotkeysList) return;
    el.modalHotkeysList.innerHTML = '';

    (tempShortcuts || []).forEach((item, index) => {
      const row = document.createElement('div');
      row.className = 'modal-hotkey-row';
      row.dataset.index = index;

      const isRecording = state.recordingHotkeyIndex === index;
      const desc = item.desc || getShortcutDefaultDesc(item.id);

      let keybindHtml = '';
      if (isRecording) {
        // Real-time MSFS 2024 modifier detection
        if (liveModifiers.ctrl || liveModifiers.alt || liveModifiers.shift) {
          const parts = [];
          if (liveModifiers.ctrl) parts.push('<kbd>Ctrl</kbd>');
          if (liveModifiers.alt) parts.push('<kbd>Alt</kbd>');
          if (liveModifiers.shift) parts.push('<kbd>Shift</kbd>');
          parts.push('<span class="recording-pulse-dot">●</span> <span class="keybind-waiting">tecla...</span>');
          keybindHtml = `<div class="keybind-caps">${parts.join('<span class="keybind-plus">+</span>')}</div>`;
        } else {
          keybindHtml = `<span class="keybind-recording-text"><span class="recording-pulse-dot">●</span> Pressione a tecla ou combinação...</span>`;
        }
      } else {
        keybindHtml = renderKeyCaps(item);
      }

      row.innerHTML = `
        <div class="modal-hotkey-info">
          <span class="modal-hotkey-name">${item.name || item.id}</span>
          <span class="modal-hotkey-desc">${desc}</span>
        </div>
        <div class="modal-hotkey-bind-wrapper">
          <button type="button" class="keybind-box ${isRecording ? 'recording' : ''}" data-index="${index}" title="Clique para redefinir tecla">
            ${keybindHtml}
          </button>
          <button type="button" class="keybind-clear-btn" data-index="${index}" title="Limpar atalho">✕</button>
        </div>
      `;

      // Click to start scanning/recording
      const keyBox = row.querySelector('.keybind-box');
      keyBox.addEventListener('click', (e) => {
        e.stopPropagation();
        if (state.recordingHotkeyIndex === index) {
          state.recordingHotkeyIndex = null;
        } else {
          state.recordingHotkeyIndex = index;
          liveModifiers = { ctrl: false, alt: false, shift: false };
        }
        renderModalHotkeys();
      });

      // Clear button
      const clearBtn = row.querySelector('.keybind-clear-btn');
      clearBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        item.ctrl = false;
        item.alt = false;
        item.shift = false;
        item.key = '';
        item.virtualKey = 0;
        if (state.recordingHotkeyIndex === index) state.recordingHotkeyIndex = null;
        renderModalHotkeys();
        showToast(`Atalho removido para "${item.name}".`);
      });

      el.modalHotkeysList.appendChild(row);
    });
  }

  function renderKeyCaps(item) {
    if (!item || !item.key) {
      return '<span class="keybind-empty">Nenhum atalho (clique para definir)</span>';
    }
    const parts = [];
    if (item.ctrl) parts.push('<kbd>Ctrl</kbd>');
    if (item.alt) parts.push('<kbd>Alt</kbd>');
    if (item.shift) parts.push('<kbd>Shift</kbd>');
    parts.push(`<kbd>${item.key}</kbd>`);
    return `<div class="keybind-caps">${parts.join('<span class="keybind-plus">+</span>')}</div>`;
  }

  function getShortcutDefaultDesc(id) {
    const found = DEFAULT_SHORTCUTS.find(s => s.id === id);
    return found ? found.desc : '';
  }

  function formatShortcutPreview(item) {
    if (!item || !item.key) return 'Nenhum';
    const parts = [];
    if (item.ctrl) parts.push('Ctrl');
    if (item.alt) parts.push('Alt');
    if (item.shift) parts.push('Shift');
    if (item.key) parts.push(item.key);
    return parts.join(' + ');
  }

  // Real-time MSFS 2024 Keybind Capture Listener
  window.addEventListener('keydown', (e) => {
    if (state.recordingHotkeyIndex === null) return;
    e.preventDefault();
    e.stopPropagation();

    // Cancel on Escape
    if (e.key === 'Escape') {
      state.recordingHotkeyIndex = null;
      liveModifiers = { ctrl: false, alt: false, shift: false };
      renderModalHotkeys();
      showToast('Configuração cancelada.');
      return;
    }

    // Clear on Backspace or Delete
    if (e.key === 'Backspace' || e.key === 'Delete') {
      const item = tempShortcuts[state.recordingHotkeyIndex];
      if (item) {
        item.ctrl = false;
        item.alt = false;
        item.shift = false;
        item.key = '';
        item.virtualKey = 0;
      }
      state.recordingHotkeyIndex = null;
      liveModifiers = { ctrl: false, alt: false, shift: false };
      renderModalHotkeys();
      showToast('Atalho removido.');
      return;
    }

    // Track modifiers in real time
    liveModifiers = {
      ctrl: e.ctrlKey,
      alt: e.altKey,
      shift: e.shiftKey
    };

    // If pure modifier key, update the real-time preview in the box!
    const isPureModifier = ['Control', 'Alt', 'Shift', 'Meta'].includes(e.key);
    if (isPureModifier) {
      renderModalHotkeys();
      return;
    }

    // Ignore CapsLock, Tab
    if (['CapsLock', 'Tab'].includes(e.key)) return;

    // A primary key was pressed! Complete the binding immediately
    const item = tempShortcuts[state.recordingHotkeyIndex];
    if (item) {
      let keyName = e.key;
      if (keyName.length === 1) keyName = keyName.toUpperCase();
      if (keyName === ' ') keyName = 'Space';
      if (keyName.startsWith('Arrow')) keyName = keyName.replace('Arrow', '');

      item.ctrl = e.ctrlKey;
      item.alt = e.altKey;
      item.shift = e.shiftKey;
      item.key = keyName;
      item.virtualKey = e.keyCode || 0;

      state.recordingHotkeyIndex = null;
      liveModifiers = { ctrl: false, alt: false, shift: false };
      renderModalHotkeys();
      showToast(`Atalho configurado: ${formatShortcutPreview(item)}`);
    }
  });

  window.addEventListener('keyup', (e) => {
    if (state.recordingHotkeyIndex === null) return;
    liveModifiers = {
      ctrl: e.ctrlKey,
      alt: e.altKey,
      shift: e.shiftKey
    };
    renderModalHotkeys();
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
        state.records = Array.isArray(msg.payload) ? msg.payload : (msg.payload?.records || []);
        if (chart) chart.setData(state.records);
        updateTimelineUI();
        break;

      case 'savedFlightsList':
        state.savedFlights = msg.payload || [];
        renderSavedFlights();
        break;

      case 'flightLoaded':
        if (msg.payload?.frameCount !== undefined) {
          state.frameCount = msg.payload.frameCount;
          state.currentFrame = 0;
          updateTimelineUI();
        }
        showToast(`Voo "${msg.payload?.aircraftTitle || msg.payload?.fileName}" carregado com sucesso!`);
        switchTab('record');
        break;

      case 'settingsData':
        if (msg.payload) {
          state.settings = {
            shortcutKeysEnabled: !!msg.payload.shortcutKeysEnabled,
            shortcuts: (msg.payload.shortcuts && msg.payload.shortcuts.length > 0)
              ? msg.payload.shortcuts.map(s => ({
                  ...s,
                  desc: s.desc || getShortcutDefaultDesc(s.id)
                }))
              : JSON.parse(JSON.stringify(DEFAULT_SHORTCUTS)),
            savedFlightsFolder: msg.payload.savedFlightsFolder || 'C:\\Program Files\\Flight Recorder\\SavedFlights',
            stayOnTop: !!msg.payload.stayOnTop,
            hudAutoHide: !!msg.payload.hudAutoHide,
            hudTranslucent: !!msg.payload.hudTranslucent
          };
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

    // Update Telemetry View Overview
    if (el.telemAircraftTitle) {
      el.telemAircraftTitle.textContent = state.aircraftTitle || 'No aircraft detected';
    }
    if (el.telemAircraftSub) {
      el.telemAircraftSub.textContent = isConn ? 'Transmissão ativa via SimConnect' : 'Inicie o MSFS para receber telemetria contínua';
    }
    if (el.telemProfileText) {
      el.telemProfileText.textContent = state.activeProfileName
        ? `${state.activeProfileName} (${state.activeProfileVarCount} vars)`
        : 'Standard SimConnect';
    }
    if (el.telemetryConnPill) {
      el.telemetryConnPill.className = `conn-pill ${isConn ? 'connected' : 'disconnected'}`;
    }
    if (el.telemetryConnText) {
      el.telemetryConnText.textContent = isConn ? 'MSFS Connected' : 'Disconnected';
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

    // Sync HUD Play/Pause button icons
    if (el.hudIconPlay && el.hudIconPause) {
      if (isReplaying) {
        el.hudIconPlay.classList.add('hidden');
        el.hudIconPause.classList.remove('hidden');
      } else {
        el.hudIconPlay.classList.remove('hidden');
        el.hudIconPause.classList.add('hidden');
      }
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

    // Sync HUD Timeline Slider and Time Counter
    if (el.hudTimelineSlider) {
      el.hudTimelineSlider.max = Math.max(0, state.frameCount - 1);
      el.hudTimelineSlider.value = state.currentFrame || 0;
      el.hudTimelineSlider.disabled = state.frameCount <= 1;
    }

    if (el.hudTimeDisplay) {
      el.hudTimeDisplay.textContent = formatTimeFromFrames(state.currentFrame || 0, state.frameCount || 0);
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
    if (el.valAlt) el.valAlt.textContent = pos.altitude ? `${Math.round(pos.altitude).toLocaleString()} ft` : '--';
    if (el.valPitch) el.valPitch.textContent = pos.pitch ? `${pos.pitch.toFixed(1)}°` : '--';
    if (el.valBank) el.valBank.textContent = pos.bank ? `${pos.bank.toFixed(1)}°` : '--';
    if (el.valHeading) {
      const hdg = Math.round(pos.trueHeading || pos.magneticHeading || 0);
      el.valHeading.textContent = (pos.trueHeading || pos.magneticHeading) ? `${hdg}°` : '--';
    }
    if (el.valGear) {
      const isDown = !!pos.gearHandlePosition;
      el.valGear.textContent = isDown ? 'DOWN' : 'UP';
      el.valGear.className = isDown ? 'telemetry-value-sm gear-badge gear-down' : 'telemetry-value-sm gear-badge gear-up';
    }
    if (el.telemContactBadge) {
      if (pos.isOnGround) {
        el.telemContactBadge.textContent = 'GROUND (GND)';
        el.telemContactBadge.className = 'flight-phase-pill phase-gnd';
      } else {
        el.telemContactBadge.textContent = 'AIRBORNE (AIR)';
        el.telemContactBadge.className = 'flight-phase-pill phase-air';
      }
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
      const throttleVal = pos.throttleLeverPosition1 !== undefined ? Math.round(pos.throttleLeverPosition1) : 0;
      el.valThrottle.textContent = (pos.throttleLeverPosition1 !== undefined) ? `${throttleVal}%` : '--';
      if (el.barThrottle) el.barThrottle.style.width = `${Math.min(100, Math.max(0, throttleVal))}%`;
    }
    if (el.valFlaps) {
      const flapsVal = pos.flapsHandleIndex !== undefined ? pos.flapsHandleIndex : 0;
      el.valFlaps.textContent = (pos.flapsHandleIndex !== undefined) ? `Pos ${flapsVal}` : '--';
      const maxFlaps = 5;
      const flapsPct = Math.min(100, Math.max(0, (flapsVal / maxFlaps) * 100));
      if (el.barFlaps) el.barFlaps.style.width = `${flapsPct}%`;
    }
  }

  // Event Listeners Initialization
  function initListeners() {
    // Navigation tabs
    el.navTabRecord?.addEventListener('click', () => switchTab('record'));
    el.navTabRecords?.addEventListener('click', () => switchTab('records'));
    el.navTabTelemetry?.addEventListener('click', () => switchTab('telemetry'));
    el.navTabSettings?.addEventListener('click', () => switchTab('settings'));

    // Telemetry Actions
    el.btnCopyCoords?.addEventListener('click', () => {
      const lat = el.valLat?.textContent;
      const lon = el.valLon?.textContent;
      if (lat && lon && lat !== '--' && lon !== '--') {
        const coords = `${lat}, ${lon}`;
        navigator.clipboard.writeText(coords);
        showToast(`Coordenadas copiadas: ${coords}`);
      } else {
        showToast('Nenhuma coordenada disponível no momento');
      }
    });

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

    // Copyable metrics
    document.querySelectorAll('.copyable').forEach((cell) => {
      cell.addEventListener('click', () => {
        const val = cell.querySelector('.telemetry-value-md, .telemetry-value-lg, .metric-value')?.textContent;
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

    // Hotkeys Modal Actions
    el.btnOpenHotkeysModal?.addEventListener('click', () => {
      tempShortcuts = JSON.parse(JSON.stringify(
        state.settings.shortcuts && state.settings.shortcuts.length > 0
          ? state.settings.shortcuts
          : DEFAULT_SHORTCUTS
      ));
      state.recordingHotkeyIndex = null;
      liveModifiers = { ctrl: false, alt: false, shift: false };
      renderModalHotkeys();
      el.hotkeysModal?.classList.remove('hidden');
    });

    const closeHotkeysModal = () => {
      state.recordingHotkeyIndex = null;
      liveModifiers = { ctrl: false, alt: false, shift: false };
      el.hotkeysModal?.classList.add('hidden');
    };

    el.btnCloseHotkeysModal?.addEventListener('click', closeHotkeysModal);
    el.btnCancelHotkeysModal?.addEventListener('click', closeHotkeysModal);

    // Click outside dialog closes modal
    el.hotkeysModal?.addEventListener('click', (e) => {
      if (e.target === el.hotkeysModal) {
        closeHotkeysModal();
      }
    });

    // Save changes from modal
    el.btnSaveHotkeysModal?.addEventListener('click', () => {
      state.settings.shortcuts = tempShortcuts;
      sendToHost('saveShortcutSettings', {
        enabled: state.settings.shortcutKeysEnabled,
        shortcuts: state.settings.shortcuts
      });
      closeHotkeysModal();
      renderSettings();
      showToast('✓ Atalhos salvos com sucesso!');
    });

    // Restore defaults inside modal
    el.btnRestoreDefaultsModal?.addEventListener('click', async () => {
      const confirmed = await showConfirmDialog({
        title: 'Restaurar Atalhos Padrão',
        message: 'Deseja restaurar todos os atalhos para os valores padrão de fábrica? As alterações não salvas serão substituídas.',
        confirmText: 'Restaurar Padrões',
        cancelText: 'Cancelar',
        variant: 'warning'
      });

      if (confirmed) {
        tempShortcuts = JSON.parse(JSON.stringify(DEFAULT_SHORTCUTS));
        renderModalHotkeys();
        showToast('Atalhos redefinidos para o padrão. Clique em Salvar para aplicar.');
      }
    });

    // Toggle global hotkeys
    el.toggleHotkeysEnabled?.addEventListener('change', (e) => {
      state.settings.shortcutKeysEnabled = e.target.checked;
      renderSettings();
      sendToHost('saveShortcutSettings', {
        enabled: state.settings.shortcutKeysEnabled,
        shortcuts: state.settings.shortcuts
      });
      showToast(e.target.checked ? 'Atalhos globais ativados!' : 'Atalhos globais desativados.');
    });

    el.toggleStayOnTopSettings?.addEventListener('change', (e) => {
      state.settings.stayOnTop = e.target.checked;
      sendToHost('setTopmost', { topmost: e.target.checked });
      showToast(e.target.checked ? 'Stay on Top: Ativado' : 'Stay on Top: Desativado');
    });
    el.btnOpenSettingsFolder?.addEventListener('click', () => sendToHost('openSavedFlightsFolder'));

    // Theme Switcher Buttons (Settings tab segmented control)
    document.querySelectorAll('#themeSegmentedGroup .theme-seg-btn').forEach((btn) => {
      btn.addEventListener('click', () => {
        const selected = btn.dataset.theme;
        if (!selected) return;
        applyTheme(selected, true);
        const labels = {
          dark: 'Modo Escuro (Flux UI) ativado',
          light: 'Modo Claro (White Mode) ativado',
          system: 'Tema sincronizado com o Windows'
        };
        showToast(labels[selected] || 'Tema alterado');
      });
    });

    // Floating Replay HUD Settings Listeners
    el.toggleHudAutoHideSettings?.addEventListener('change', (e) => {
      const autoHide = e.target.checked;
      state.settings.hudAutoHide = autoHide;
      sendToHost('saveHudSettings', { autoHide });
      showToast(autoHide ? 'Auto-ocultar ativado para o Mini Player.' : 'Auto-ocultar desativado para o Mini Player.');
    });

    el.hudStyleSegmentedGroup?.addEventListener('click', (e) => {
      const btn = e.target.closest('.theme-seg-btn');
      if (!btn) return;
      const style = btn.getAttribute('data-hud-style');
      if (!style) return;
      const isTranslucent = style === 'translucent';
      state.settings.hudTranslucent = isTranslucent;
      el.hudStyleSegmentedGroup.querySelectorAll('.theme-seg-btn').forEach(b => {
        b.classList.toggle('active', b === btn);
      });
      sendToHost('saveHudSettings', { translucent: isTranslucent });
      showToast(isTranslucent ? 'Mini Player: Translúcido (Meio transparente)' : 'Mini Player: Sólido');
    });

    // ==============================================
    // FLOATING REPLAY HUD LAUNCHER
    // ==============================================
    el.btnOpenReplayHud?.addEventListener('click', () => {
      sendToHost('openReplayHud');
      showToast('Barra de controle de replay aberta no topo da tela.');
    });

    // Global click closes dropdowns and cancels hotkey scanning if clicking outside keybind box
    document.addEventListener('click', (e) => {
      if (state.recordingHotkeyIndex !== null && !e.target.closest('.keybind-box')) {
        state.recordingHotkeyIndex = null;
        liveModifiers = { ctrl: false, alt: false, shift: false };
        renderModalHotkeys();
      }
      closeAllMenus();
    });

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
  initTheme();
  initListeners();
  render();
  renderSettings();

  // Signal host that UI is ready
  sendToHost('uiReady');
})();
