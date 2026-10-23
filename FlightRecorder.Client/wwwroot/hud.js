(() => {
  'use strict';

  const state = {
    currentFrame: 0,
    frameCount: 0,
    machineState: 'IdleEmpty'
  };

  let isDragging = false;

  const el = {
    hudBar: document.getElementById('hudBar'),
    btnHudClose: document.getElementById('btnHudClose'),
    btnHudDrag: document.getElementById('btnHudDrag'),

    btnFastBack: document.getElementById('btnFastBack'),
    btnStepBack10: document.getElementById('btnStepBack10'),
    btnStepBack1: document.getElementById('btnStepBack1'),
    btnPlayPause: document.getElementById('btnPlayPause'),
    iconPlay: document.getElementById('iconPlay'),
    iconPause: document.getElementById('iconPause'),
    btnStepFwd1: document.getElementById('btnStepFwd1'),
    btnStepFwd10: document.getElementById('btnStepFwd10'),
    btnFastFwd: document.getElementById('btnFastFwd'),

    timeDisplay: document.getElementById('timeDisplay'),
    sliderScrub: document.getElementById('sliderScrub')
  };

  function sendAction(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({ action, ...data });
    }
  }

  function formatTime(frame) {
    const totalSeconds = (frame || 0) / 60;
    const mins = Math.floor(totalSeconds / 60);
    const secs = Math.floor(totalSeconds % 60);
    const hundredths = Math.floor((totalSeconds % 1) * 100);
    const mStr = String(mins).padStart(2, '0');
    const sStr = String(secs).padStart(2, '0');
    const hStr = String(hundredths).padStart(2, '0');
    return `${mStr}:${sStr}.${hStr}`;
  }

  function updateUI() {
    if (el.sliderScrub) {
      el.sliderScrub.max = Math.max(0, state.frameCount - 1);
      el.sliderScrub.value = state.currentFrame || 0;
      el.sliderScrub.disabled = state.frameCount <= 1;
    }

    if (el.timeDisplay) {
      el.timeDisplay.textContent = formatTime(state.currentFrame || 0);
    }

    const isReplaying = (state.machineState || '').startsWith('Replaying');
    if (el.iconPlay && el.iconPause) {
      if (isReplaying) {
        el.iconPlay.classList.add('hidden');
        el.iconPause.classList.remove('hidden');
      } else {
        el.iconPlay.classList.remove('hidden');
        el.iconPause.classList.add('hidden');
      }
    }
  }

  // Theme Sync
  const savedTheme = localStorage.getItem('flightrecorder.theme') || 'dark';
  let effectiveTheme = savedTheme;
  if (savedTheme === 'system') {
    const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    effectiveTheme = prefersDark ? 'dark' : 'light';
  }
  document.documentElement.className = effectiveTheme;

  // Window drag via Win32 message
  const triggerDrag = (e) => {
    if (e.target.closest('button, input, select, a')) return;
    isDragging = true;
    sendAction('dragWindow');
  };

  window.addEventListener('mouseup', () => {
    isDragging = false;
  });

  el.btnHudDrag?.addEventListener('mousedown', triggerDrag);
  el.hudBar?.querySelector('.hud-window-top')?.addEventListener('mousedown', triggerDrag);

  // Close
  el.btnHudClose?.addEventListener('click', () => {
    sendAction('close');
  });

  // Play / Pause
  el.btnPlayPause?.addEventListener('click', () => {
    const s = state.machineState || '';
    if (s.startsWith('Replaying')) {
      sendAction('pause');
    } else if (s.startsWith('Pausing')) {
      sendAction('resume');
    } else {
      sendAction('play');
    }
  });

  // Steps & Jumps
  el.btnFastBack?.addEventListener('click', () => sendAction('step', { delta: -300 }));
  el.btnStepBack10?.addEventListener('click', () => sendAction('step', { delta: -10 }));
  el.btnStepBack1?.addEventListener('click', () => sendAction('step', { delta: -1 }));
  el.btnStepFwd1?.addEventListener('click', () => sendAction('step', { delta: 1 }));
  el.btnStepFwd10?.addEventListener('click', () => sendAction('step', { delta: 10 }));
  el.btnFastFwd?.addEventListener('click', () => sendAction('step', { delta: 300 }));

  // Scrub Slider
  el.sliderScrub?.addEventListener('input', (e) => {
    const target = parseInt(e.target.value, 10);
    state.currentFrame = target;
    updateUI();
    sendAction('seek', { frame: target });
  });

  // Mouse Enter, Move & Leave for Auto-Hide
  window.addEventListener('mouseenter', () => {
    sendAction('mouseEnter');
  });

  window.addEventListener('mousemove', () => {
    sendAction('mouseEnter');
  });

  window.addEventListener('mouseleave', () => {
    if (isDragging) return;
    sendAction('mouseLeave');
  });

  // Host Message Dispatcher
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      const msg = event.data;
      if (!msg) return;

      if (msg.type === 'stateUpdate' && msg.payload) {
        if (msg.payload.currentFrame !== undefined) state.currentFrame = msg.payload.currentFrame;
        if (msg.payload.frameCount !== undefined) state.frameCount = msg.payload.frameCount;
        if (msg.payload.machineState !== undefined) state.machineState = msg.payload.machineState;
        updateUI();
      } else if (msg.type === 'themeChanged' && msg.payload) {
        document.documentElement.className = msg.payload.effectiveTheme || msg.payload.theme || 'dark';
      } else if (msg.type === 'hudConfigUpdate' && msg.payload) {
        if (msg.payload.translucent !== undefined) {
          document.body.classList.toggle('translucent', !!msg.payload.translucent);
        }
      } else if (msg.type === 'hudVisibilityUpdate' && msg.payload) {
        if (msg.payload.visible !== undefined) {
          document.body.classList.toggle('hud-hidden', !msg.payload.visible);
        }
      }
    });
  }

  // Signal C# that HUD window webView is ready
  sendAction('hudReady');
  updateUI();
})();
