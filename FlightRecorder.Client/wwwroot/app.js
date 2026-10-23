/**
 * Flight Recorder - Flux UI Application Logic
 * Bridges WebView2 and C# State Machine
 */

(function () {
  'use strict';

  // State cache
  const state = {
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
    simState: {}
  };

  // DOM Elements
  const el = {
    // Status
    statusBanner: document.getElementById('statusBanner'),
    statusDot: document.getElementById('statusDot'),
    statusText: document.getElementById('statusText'),

    // Aircraft Card & Profile
    aircraftTitle: document.getElementById('aircraftTitle'),
    aircraftFile: document.getElementById('aircraftFile'),
    saveBadge: document.getElementById('saveBadge'),
    profileBadge: document.getElementById('profileBadge'),
    profileText: document.getElementById('profileText'),
    valProfileDetail: document.getElementById('valProfileDetail'),

    // Primary Actions
    btnRecord: document.getElementById('btnRecord'),
    btnStopRecord: document.getElementById('btnStopRecord'),
    btnReplay: document.getElementById('btnReplay'),
    btnPauseReplay: document.getElementById('btnPauseReplay'),
    btnResumeReplay: document.getElementById('btnResumeReplay'),
    btnStopReplay: document.getElementById('btnStopReplay'),
    btnReplayAI: document.getElementById('btnReplayAI'),

    // Timeline & Scrub
    timelineSlider: document.getElementById('timelineSlider'),
    currentFrameText: document.getElementById('currentFrameText'),
    totalFramesText: document.getElementById('totalFramesText'),
    chartOverlay: document.getElementById('chartOverlay'),

    // Playback modifiers
    btnRepeat: document.getElementById('btnRepeat'),
    btnSpeed: document.getElementById('btnSpeed'),
    speedLabel: document.getElementById('speedLabel'),
    speedMenu: document.getElementById('speedMenu'),
    btnTrim: document.getElementById('btnTrim'),
    trimMenu: document.getElementById('trimMenu'),

    // Storage & Export
    btnSave: document.getElementById('btnSave'),
    btnLoad: document.getElementById('btnLoad'),
    btnExport: document.getElementById('btnExport'),
    exportMenu: document.getElementById('exportMenu'),

    // Telemetry
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

    // Utilities
    btnTopmost: document.getElementById('btnTopmost'),
    btnShortcuts: document.getElementById('btnShortcuts'),
    btnMinimize: document.getElementById('btnMinimize'),
    btnClose: document.getElementById('btnClose'),

    // Modals & Toasts
    shortcutsModal: document.getElementById('shortcutsModal'),
    btnCloseModal: document.getElementById('btnCloseModal'),
    toast: document.getElementById('toast'),
    flightChartCanvas: document.getElementById('flightChart')
  };

  // Initialize Canvas Chart
  const chart = new FlightChart(el.flightChartCanvas, (targetFrame) => {
    seekToFrame(targetFrame);
  });

  // Post message to C# backend
  function sendToHost(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({ action, ...data });
    } else {
      console.log('[DEBUG] Mock sendToHost:', action, data);
    }
  }

  // Toast Notification
  let toastTimer = null;
  function showToast(msg) {
    if (!el.toast) return;
    el.toast.textContent = msg;
    el.toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      el.toast.classList.remove('show');
    }, 2200);
  }

  // Handle updates coming from C#
  function handleHostMessage(msg) {
    if (!msg) return;

    if (msg.type === 'stateUpdate') {
      applyStateUpdate(msg.payload);
    } else if (msg.type === 'telemetryUpdate') {
      applyTelemetryUpdate(msg.payload);
    } else if (msg.type === 'recordsUpdate') {
      state.records = msg.payload || [];
      chart.setData(state.records);
      updateChartOverlay();
    } else if (msg.type === 'toast') {
      showToast(msg.payload);
    } else if (msg.type === 'updateStatus') {
      handleUpdateStatus(msg.payload);
    } else if (msg.type === 'updateProgress') {
      handleUpdateProgress(msg.payload);
    }
  }

  function handleUpdateStatus(payload) {
    const banner = document.getElementById('updateBanner');
    const text = document.getElementById('updateBannerText');
    const fill = document.getElementById('updateProgressFill');
    if (!banner || !text) return;

    if (payload.status === 'downloading') {
      banner.classList.remove('hidden');
      text.textContent = `Nova versão disponível (${payload.version}). Baixando atualização...`;
      if (fill) fill.style.width = '10%';
    } else if (payload.status === 'ready') {
      banner.classList.remove('hidden');
      text.textContent = `Atualização ${payload.version} pronta! Reiniciando aplicativo...`;
      if (fill) fill.style.width = '100%';
    }
  }

  function handleUpdateProgress(payload) {
    const banner = document.getElementById('updateBanner');
    const text = document.getElementById('updateBannerText');
    const fill = document.getElementById('updateProgressFill');
    if (!banner || !text) return;

    banner.classList.remove('hidden');
    text.textContent = `Baixando atualização ${payload.version} (${payload.progress}%)...`;
    if (fill) fill.style.width = `${Math.max(10, payload.progress)}%`;
  }

  function applyStateUpdate(payload) {
    if (payload.machineState !== undefined) state.machineState = payload.machineState;
    if (payload.simConnectState !== undefined) state.simConnectState = payload.simConnectState;
    if (payload.currentFrame !== undefined) {
      state.currentFrame = payload.currentFrame;
      chart.setCurrentFrame(payload.currentFrame);
    }
    if (payload.frameCount !== undefined) state.frameCount = payload.frameCount;
    if (payload.aircraftTitle !== undefined) state.aircraftTitle = payload.aircraftTitle;
    if (payload.fileName !== undefined) state.fileName = payload.fileName;
    if (payload.activeProfileName !== undefined) state.activeProfileName = payload.activeProfileName;
    if (payload.activeProfileVendor !== undefined) state.activeProfileVendor = payload.activeProfileVendor;
    if (payload.activeProfileVarCount !== undefined) state.activeProfileVarCount = payload.activeProfileVarCount;
    if (payload.speed !== undefined) state.speed = payload.speed;
    if (payload.repeat !== undefined) state.repeat = payload.repeat;
    if (payload.topmost !== undefined) state.topmost = payload.topmost;

    render();
  }

  function applyTelemetryUpdate(payload) {
    if (payload.position) {
      state.aircraftPosition = payload.position;
      updateTelemetryUI();
    }
  }

  // UI Re-render based on State Machine
  function render() {
    const s = state.machineState;
    const isConn = state.simConnectState === 'Connected';
    const isRecording = s === 'Recording';
    const isReplaying = s === 'ReplayingSaved' || s === 'ReplayingUnsaved';
    const isPausing = s === 'PausingSaved' || s === 'PausingUnsaved';
    const hasData = state.frameCount > 0;
    const isUnsaved = s.includes('Unsaved');

    // Status Banner
    if (isRecording) {
      el.statusBanner.className = 'status-banner recording';
      el.statusDot.className = 'status-dot pulse';
      el.statusText.textContent = `Recording flight in progress (${state.frameCount} frames)...`;
    } else if (isConn) {
      el.statusBanner.className = 'status-banner connected';
      el.statusDot.className = 'status-dot';
      el.statusText.textContent = isReplaying
        ? `Replaying flight (${state.speed}x speed)`
        : isPausing
        ? `Playback paused at frame ${state.currentFrame + 1}`
        : 'Connected to Microsoft Flight Simulator';
    } else {
      el.statusBanner.className = 'status-banner disconnected';
      el.statusDot.className = 'status-dot';
      el.statusText.textContent = 'SimConnect Disconnected — Please start MSFS';
    }

    // Aircraft Card
    el.aircraftTitle.textContent = state.aircraftTitle || (isConn ? 'Ready to Record' : 'No aircraft detected');
    el.aircraftFile.textContent = state.fileName ? `File: ${state.fileName}` : hasData ? 'Unsaved session' : 'No active recording';

    if (hasData) {
      el.saveBadge.style.display = 'inline-flex';
      if (isUnsaved) {
        el.saveBadge.className = 'save-badge unsaved';
        el.saveBadge.innerHTML = '● Unsaved';
      } else {
        el.saveBadge.className = 'save-badge saved';
        el.saveBadge.innerHTML = '✓ Saved';
      }
    } else {
      el.saveBadge.style.display = 'none';
    }

    // Profile Badge & Detail
    if (el.profileBadge && el.profileText) {
      if (state.activeProfileName) {
        el.profileBadge.className = 'profile-badge active';
        el.profileText.textContent = `${state.activeProfileName} (${state.activeProfileVarCount} L-Vars)`;
        el.profileBadge.title = `Perfil Ativo: ${state.activeProfileName} (${state.activeProfileVarCount} variáveis mapeadas sem WASM)`;
        if (el.valProfileDetail) {
          el.valProfileDetail.textContent = `${state.activeProfileName} (${state.activeProfileVarCount} vars)`;
          el.valProfileDetail.style.color = '#34d399';
        }
      } else {
        el.profileBadge.className = 'profile-badge standard';
        el.profileText.textContent = 'Perfil Standard';
        el.profileBadge.title = 'Perfil Genérico (SimConnect padrão, sem L-Vars personalizadas)';
        if (el.valProfileDetail) {
          el.valProfileDetail.textContent = 'Standard SimConnect';
          el.valProfileDetail.style.color = '#38bdf8';
        }
      }
    }

    // Record buttons
    const canRecord = isConn && ['IdleEmpty', 'IdleSaved', 'IdleUnsaved'].includes(s);
    el.btnRecord.disabled = !canRecord;
    el.btnRecord.style.display = isRecording ? 'none' : 'inline-flex';

    el.btnStopRecord.disabled = !isRecording;
    el.btnStopRecord.style.display = isRecording ? 'inline-flex' : 'none';

    // Replay controls
    const canReplay = isConn && ['IdleSaved', 'IdleUnsaved'].includes(s);
    el.btnReplay.disabled = !canReplay;

    el.btnPauseReplay.disabled = !isReplaying;
    el.btnResumeReplay.disabled = !isPausing;
    el.btnStopReplay.disabled = !(isReplaying || isPausing);

    // Save & Load
    const canSave = ['DisconnectedSaved', 'IdleSaved', 'DisconnectedUnsaved', 'IdleUnsaved'].includes(s);
    el.btnSave.disabled = !canSave;
    el.btnSave.innerHTML = isUnsaved
      ? `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg> Save*`
      : `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg> Save`;

    const canLoad = ['DisconnectedEmpty', 'DisconnectedSaved', 'DisconnectedUnsaved', 'IdleEmpty', 'IdleSaved', 'IdleUnsaved'].includes(s);
    el.btnLoad.disabled = !canLoad;
    el.btnExport.disabled = !hasData;

    // Timeline Slider
    const canScrub = ['DisconnectedSaved', 'DisconnectedUnsaved', 'IdleSaved', 'IdleUnsaved', 'PausingSaved', 'PausingUnsaved'].includes(s);
    el.timelineSlider.disabled = !canScrub;
    el.timelineSlider.max = Math.max(0, state.frameCount - 1);
    el.timelineSlider.value = state.currentFrame;

    el.currentFrameText.textContent = hasData ? (state.currentFrame + 1).toLocaleString() : '0';
    el.totalFramesText.textContent = state.frameCount.toLocaleString();

    // Tools & Toggles
    el.btnRepeat.classList.toggle('active', state.repeat);
    el.speedLabel.textContent = `x${state.speed}`;
    el.btnTopmost.classList.toggle('active', state.topmost);

    const canTrim = ['IdleSaved', 'IdleUnsaved', 'DisconnectedSaved', 'DisconnectedUnsaved', 'PausingSaved', 'PausingUnsaved'].includes(s);
    el.btnTrim.disabled = !canTrim;

    updateChartOverlay();
  }

  function updateChartOverlay() {
    const s = state.machineState;
    if (s === 'Recording') {
      el.chartOverlay.style.display = 'flex';
      el.chartOverlay.textContent = 'Recording incoming telemetric stream...';
    } else if (s.includes('Saving')) {
      el.chartOverlay.style.display = 'flex';
      el.chartOverlay.textContent = 'Saving flight file to disk...';
    } else if (s.includes('Loading')) {
      el.chartOverlay.style.display = 'flex';
      el.chartOverlay.textContent = 'Loading flight recording...';
    } else if (state.frameCount === 0) {
      el.chartOverlay.style.display = 'flex';
      el.chartOverlay.textContent = 'No flight recorded or loaded';
    } else {
      el.chartOverlay.style.display = 'none';
    }
  }

  function updateTelemetryUI() {
    const p = state.aircraftPosition || {};

    el.valLat.textContent = p.latitude != null ? p.latitude.toFixed(6) : '--';
    el.valLon.textContent = p.longitude != null ? p.longitude.toFixed(6) : '--';
    el.valAlt.textContent = p.altitude != null ? `${Math.round(p.altitude).toLocaleString()} ft` : '--';
    el.valPitch.textContent = p.pitch != null ? `${p.pitch.toFixed(1)}°` : '--';
    el.valBank.textContent = p.bank != null ? `${p.bank.toFixed(1)}°` : '--';
    el.valHeading.textContent = p.trueHeading != null ? `${Math.round(p.trueHeading)}°` : '--';
    el.valGear.textContent = p.gearHandlePosition != null ? (p.gearHandlePosition > 0.5 ? 'DOWN' : 'UP') : '--';
    el.valGround.textContent = p.isOnGround ? 'GROUND' : 'AIR';
    el.valGround.style.color = p.isOnGround ? '#f59e0b' : '#34d399';

    if (p.touchdownNormalVelocity != null && p.touchdownNormalVelocity !== 0) {
      el.valLandingRate.textContent = `${Math.round(p.touchdownNormalVelocity * 60)} fpm`;
    } else {
      el.valLandingRate.textContent = '--';
    }

    el.valThrottle.textContent = p.throttleLeverPosition1 != null ? `${Math.round(p.throttleLeverPosition1)}%` : '--';
    el.valFlaps.textContent = p.flapsHandleIndex != null ? `Pos ${p.flapsHandleIndex}` : '--';
  }

  function seekToFrame(frame) {
    state.currentFrame = frame;
    el.timelineSlider.value = frame;
    el.currentFrameText.textContent = (frame + 1).toLocaleString();
    chart.setCurrentFrame(frame);
    sendToHost('seek', { frame });
  }

  // Setup Event Listeners
  function initListeners() {
    // Actions
    el.btnRecord.addEventListener('click', () => sendToHost('record'));
    el.btnStopRecord.addEventListener('click', () => sendToHost('stop'));
    el.btnReplay.addEventListener('click', () => sendToHost('replay'));
    el.btnPauseReplay.addEventListener('click', () => sendToHost('pause'));
    el.btnResumeReplay.addEventListener('click', () => sendToHost('resume'));
    el.btnStopReplay.addEventListener('click', () => sendToHost('stopReplay'));
    el.btnReplayAI.addEventListener('click', () => sendToHost('replayAI'));

    // Save & Load
    el.btnSave.addEventListener('click', () => sendToHost('save'));
    el.btnLoad.addEventListener('click', () => sendToHost('load'));

    // Timeline Slider
    el.timelineSlider.addEventListener('input', (e) => {
      seekToFrame(parseInt(e.target.value, 10));
    });

    // Repeat Toggle
    el.btnRepeat.addEventListener('click', () => {
      state.repeat = !state.repeat;
      el.btnRepeat.classList.toggle('active', state.repeat);
      sendToHost('setRepeat', { repeat: state.repeat });
      showToast(state.repeat ? 'Repeat enabled' : 'Repeat disabled');
    });

    // Speed Popover
    el.btnSpeed.addEventListener('click', (e) => {
      e.stopPropagation();
      closeAllMenus();
      el.speedMenu.classList.toggle('show');
    });

    document.querySelectorAll('[data-speed]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        const val = parseFloat(e.currentTarget.dataset.speed);
        state.speed = val;
        el.speedLabel.textContent = `x${val}`;
        sendToHost('setSpeed', { rate: val });
        el.speedMenu.classList.remove('show');
        showToast(`Replay speed: x${val}`);
      });
    });

    // Trim Popover
    el.btnTrim.addEventListener('click', (e) => {
      e.stopPropagation();
      closeAllMenus();
      el.trimMenu.classList.toggle('show');
    });

    document.getElementById('btnTrimStart').addEventListener('click', () => {
      sendToHost('trimStart');
      el.trimMenu.classList.remove('show');
      showToast('Trimmed start of recording');
    });

    document.getElementById('btnTrimEnd').addEventListener('click', () => {
      sendToHost('trimEnd');
      el.trimMenu.classList.remove('show');
      showToast('Trimmed end of recording');
    });

    // Export Popover
    el.btnExport.addEventListener('click', (e) => {
      e.stopPropagation();
      closeAllMenus();
      el.exportMenu.classList.toggle('show');
    });

    document.getElementById('btnExportCsv').addEventListener('click', () => {
      sendToHost('export', { format: 'csv' });
      el.exportMenu.classList.remove('show');
    });

    document.getElementById('btnExportKml').addEventListener('click', () => {
      sendToHost('export', { format: 'kml' });
      el.exportMenu.classList.remove('show');
    });

    // Telemetry Accordion
    el.telemetryHeader.addEventListener('click', () => {
      state.telemetryExpanded = !state.telemetryExpanded;
      el.telemetryBody.classList.toggle('collapsed', !state.telemetryExpanded);
      el.telemetryChevron.style.transform = state.telemetryExpanded ? 'rotate(180deg)' : 'rotate(0deg)';
      sendToHost('telemetryToggle', { expanded: state.telemetryExpanded });
    });

    // Click to copy coordinates
    document.querySelectorAll('.copyable').forEach((cell) => {
      cell.addEventListener('click', () => {
        const val = cell.querySelector('.metric-value')?.textContent;
        if (val && val !== '--') {
          navigator.clipboard.writeText(val);
          showToast(`Copied: ${val}`);
        }
      });
    });

    // Utility buttons
    if (el.btnOpenProfiles) {
      el.btnOpenProfiles.addEventListener('click', (e) => {
        e.stopPropagation();
        sendToHost('openProfilesFolder');
        showToast('Pasta de perfis aberta no Windows Explorer');
      });
    }

    el.btnTopmost.addEventListener('click', () => {
      state.topmost = !state.topmost;
      el.btnTopmost.classList.toggle('active', state.topmost);
      sendToHost('setTopmost', { topmost: state.topmost });
      showToast(state.topmost ? 'Always on Top: ON' : 'Always on Top: OFF');
    });

    el.btnShortcuts.addEventListener('click', () => {
      el.shortcutsModal.classList.add('show');
    });

    el.btnCloseModal.addEventListener('click', () => {
      el.shortcutsModal.classList.remove('show');
    });

    el.shortcutsModal.addEventListener('click', (e) => {
      if (e.target === el.shortcutsModal) el.shortcutsModal.classList.remove('show');
    });

    // Global click closes dropdowns
    document.addEventListener('click', () => closeAllMenus());

    function closeAllMenus() {
      el.speedMenu?.classList.remove('show');
      el.trimMenu?.classList.remove('show');
      el.exportMenu?.classList.remove('show');
    }
  }

  // WebView2 Message Bridge Registration
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      handleHostMessage(event.data);
    });
  }

  // Initial setup
  initListeners();
  render();

  // Signal C# that UI is ready to receive state
  sendToHost('uiReady');
})();
