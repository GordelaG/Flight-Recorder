/**
 * Flight Recorder - English (US) Language Pack
 * Code: 'en'
 */
(function (root) {
  'use strict';

  const locale = {
    lang: 'en',
    name: 'English',
    flag: '🇺🇸',
    sub: 'Default (US)',
    translations: {
      nav: {
        record: 'Record',
        recordTitle: 'Flight Recorder & Replay Panel',
        records: 'Records',
        recordsTitle: 'Saved Flights List',
        telemetry: 'Telemetry',
        telemetryTitle: 'Live Telemetry & Flight Instruments',
        settings: 'Settings',
        settingsTitle: 'Settings & Keyboard Shortcuts'
      },
      update: {
        downloading: 'Update found. Downloading...',
        ready: 'Update ready. Restart to apply.'
      },
      status: {
        connecting: 'Connecting to Microsoft Flight Simulator...',
        disconnected: 'SimConnect Disconnected — Please start MSFS',
        connected: 'Connected to Microsoft Flight Simulator',
        recording: 'Recording Flight...',
        replaying: 'Replaying Flight...',
        paused: 'Replay Paused',
        idleSaved: 'Flight Saved',
        idleUnsaved: 'Unsaved Recording'
      },
      aircraft: {
        label: 'AIRCRAFT',
        none: 'No aircraft detected',
        noRecording: 'No active recording',
        recordingActive: 'Recording active',
        profileStd: 'Standard Profile',
        profileCustom: 'Custom Profile',
        profileTooltip: 'Custom variables recording/replay system (Native SimConnect - Zero WASM)',
        savedBadge: '✓ Saved'
      },
      controls: {
        recordFlight: 'Record Flight',
        stopRecording: 'Stop Recording',
        replay: 'Replay',
        replayAiTitle: 'Replay options (AI formation plane)',
        pause: 'Pause',
        resume: 'Resume',
        stop: 'Stop',
        loop: 'Loop',
        loopActiveToast: 'Loop replay enabled',
        loopInactiveToast: 'Loop replay disabled',
        speed: 'Speed',
        speedToast: 'Replay speed: x{rate}',
        trim: 'Trim',
        trimStart: 'Trim Start (from current frame)',
        trimEnd: 'Trim End (up to current frame)',
        trimStartToast: 'Flight start trimmed to current frame',
        trimEndToast: 'Flight end trimmed to current frame',
        frame: 'Frame',
        noFlight: 'No flight recorded or loaded',
        save: 'Save to SavedFlights',
        load: 'Load from Records',
        hudLauncher: 'Floating Replay Control Bar (HUD)',
        topmostBadge: 'TOPMOST'
      },
      records: {
        title: 'Saved Flights',
        desc: 'Recorded flights history and management',
        searchPlaceholder: 'Search by aircraft or filename...',
        refresh: 'Refresh',
        refreshToast: 'Flight list refreshed',
        import: 'Import',
        openFolder: 'Open Folder',
        statTotal: 'Total Flights',
        statTime: 'Recorded Time',
        minutesUnit: 'min',
        emptyTitle: 'No saved flights found',
        emptyDesc: 'Record a flight in the simulator and click Save to store it here.',
        emptyFilterDesc: 'No results match your search.',
        load: 'Load',
        loadTitle: 'Load this flight for Replay',
        deleteTitle: 'Delete recording',
        confirmDeleteTitle: 'Delete Saved Flight',
        confirmDeleteMsg: 'Are you sure you want to permanently delete the flight "{name}"? This action cannot be undone.',
        confirmDeleteBtn: 'Delete Flight',
        framesSuffix: 'frames',
        flightLoadedToast: 'Flight "{name}" loaded successfully!'
      },
      telemetry: {
        title: 'Live Flight Telemetry',
        desc: 'Real-time parameters via SimConnect',
        copyCoords: 'Copy Coords',
        coordsCopiedToast: 'Coordinates copied: {coords}',
        noCoordsToast: 'No coordinates available right now',
        valCopiedToast: 'Copied: {val}',
        pitch: 'Pitch',
        bank: 'Bank',
        heading: 'Heading',
        gear: 'Landing Gear',
        throttle: 'Throttle',
        landingRate: 'Landing Rate',
        flaps: 'Flaps',
        connectedAircraft: 'CONNECTED AIRCRAFT',
        contactStatus: 'CONTACT STATUS',
        latitude: 'Latitude',
        longitude: 'Longitude',
        altitude: 'Altitude',
        gnd: 'GROUND (GND)',
        air: 'AIRBORNE (AIR)',
        gearDown: 'DOWN',
        gearUp: 'UP',
        chartPlaceholder: 'Telemetry chart will appear once flight has frames'
      },
      settings: {
        title: 'Settings',
        subtitle: 'Customize display language, appearance, keyboard shortcuts and Flight Recorder preferences',
        languageTitle: 'Language / Idioma',
        languageDesc: 'Choose the display language for Flight Recorder.',
        languageToast: 'Language changed to English',
        themeTitle: 'Appearance & Theme',
        themeDesc: 'Switch between Flux UI Dark theme, modern Light theme, or Windows sync.',
        themeDark: 'Dark',
        themeLight: 'Light',
        themeSystem: 'System',
        themeDarkToast: 'Dark Mode (Flux UI) activated',
        themeLightToast: 'Light Mode activated',
        themeSystemToast: 'Theme synchronized with Windows',
        hotkeysTitle: 'Global Keyboard Shortcuts',
        hotkeysDesc: 'Control recording and replay directly from MSFS using keyboard shortcuts.',
        hotkeysEnable: 'Enable Global Shortcuts',
        hotkeysEnableDesc: 'Configured hotkeys will work directly inside MSFS 2020 / 2024 even in full screen.',
        hotkeysDisabledDesc: 'Shortcuts disabled: simulator will not respond to keys while this option is off.',
        hotkeysActive: 'Enabled',
        hotkeysInactive: 'Disabled',
        hotkeysEnabledToast: 'Global shortcuts enabled in MSFS!',
        hotkeysDisabledToast: 'Global shortcuts disabled.',
        hotkeysCombos: 'Key Combinations',
        hotkeysActiveCount: '{count} active shortcuts',
        hotkeysInactiveCount: '{count} shortcuts (disabled)',
        hotkeysConfigureBtn: 'Configure Shortcuts',
        folderTitle: 'Recordings Directory (SavedFlights)',
        folderDesc: 'Folder where flights are automatically stored when clicking Save.',
        folderOpenBtn: 'Open in Explorer',
        autoRecordTitle: 'Auto-Record Below Altitude',
        autoRecordDesc: 'Automatically starts recording when the app is open and the aircraft flies below the altitude threshold (ideal for landings and approaches).',
        autoRecordTriggerAlt: 'Trigger Altitude',
        autoRecordRangeDesc: 'Configurable between 1,000 and 15,000 ft (default: 6,000 ft). Recording starts when descending below this limit.',
        autoRecordStatusActive: 'Enabled (< {alt})',
        autoRecordStatusInactive: 'Disabled',
        autoRecordDefault: 'Default',
        autoRecordEnabledToast: 'Auto-recording below {alt} ENABLED',
        autoRecordDisabledToast: 'Auto-recording by altitude DISABLED',
        autoRecordSetToast: 'Auto-record trigger altitude set to {alt}',
        stayOnTopTitle: 'Stay on Top',
        stayOnTopDesc: 'Keep Flight Recorder pinned above other windows and the simulator.',
        stayOnTopActiveToast: 'Stay on Top: Enabled',
        stayOnTopInactiveToast: 'Stay on Top: Disabled',
        hudTitle: 'Floating Replay Mini-Player (HUD)',
        hudDesc: 'Display and behavior options for the in-sim floating controller.',
        hudAutoHide: 'Auto-hide when paused',
        hudAutoHideDesc: 'Automatically fades out the mini player when replay is paused or stopped.',
        hudStyle: 'HUD Visual Style',
        hudStyleDesc: 'Choose between solid dark or glassmorphism translucent style.',
        hudStyleSolid: 'Solid Dark',
        hudStyleTranslucent: 'Translucent (Glass)'
      },
      hotkeys: {
        modalTitle: 'Configure Global Shortcuts',
        modalSubtitle: 'Click Record to capture the key combination on your keyboard.',
        colAction: 'Action',
        colCurrent: 'Current Shortcut',
        colCapture: 'Capture',
        btnRestore: 'Restore Defaults',
        btnCancel: 'Cancel',
        btnSave: 'Save Shortcuts',
        btnSaveToast: '✓ Shortcuts saved successfully!',
        recordKey: 'Record Key',
        recording: 'Press keys...',
        savedToast: 'Shortcuts reset to default. Click Save to apply.',
        cancelledToast: 'Configuration cancelled.',
        shortcutRemovedToast: 'Shortcut removed for "{name}".',
        shortcutSetToast: 'Shortcut configured: {combo}',
        confirmRestoreTitle: 'Restore Default Shortcuts',
        confirmRestoreMsg: 'Do you want to restore all shortcuts to factory defaults? Unsaved changes will be discarded.',
        confirmRestoreBtn: 'Restore Defaults',
        Record: {
          name: 'Record Flight (Record)',
          desc: 'Starts recording aircraft flight positions'
        },
        StopRecording: {
          name: 'Stop Recording (Stop)',
          desc: 'Stops the active recording'
        },
        Replay: {
          name: 'Replay Flight (Play)',
          desc: 'Starts playback of the recorded flight'
        },
        Pause: {
          name: 'Pause Replay (Pause)',
          desc: 'Freezes simulator replay playback'
        },
        Resume: {
          name: 'Resume Replay (Resume)',
          desc: 'Resumes paused playback'
        },
        StopReplay: {
          name: 'Stop Replay (Stop)',
          desc: 'Ends replay and restores simulator controls'
        },
        SaveToDisk: {
          name: 'Quick Save Flight (Save)',
          desc: 'Saves recording directly to SavedFlights folder'
        },
        ToggleReplayHud: {
          name: 'Toggle Replay HUD (HUD)',
          desc: 'Shows or hides the floating replay mini-player'
        }
      },
      dialog: {
        cancel: 'Cancel',
        confirm: 'Confirm',
        attention: 'Attention',
        understood: 'OK',
        unsavedTitle: 'Unsaved Recording',
        unsavedRecordMsg: 'You have an unsaved recorded flight.\nStarting a new recording will replace the current one.\n\nDo you want to proceed?',
        unsavedRecordBtn: 'Discard and Record',
        unsavedLoadMsg: 'You have an unsaved recorded flight.\nLoading another flight will replace the current one.\n\nDo you want to proceed?',
        unsavedLoadBtn: 'Discard and Load',
        trimStartTitle: 'Trim Flight Start',
        trimStartMsg: 'You have an unsaved flight.\nTrimming will permanently remove all frames before the current point.\n\nDo you want to proceed?',
        trimEndTitle: 'Trim Flight End',
        trimEndMsg: 'You have an unsaved flight.\nTrimming will permanently remove all frames after the current point.\n\nDo you want to proceed?',
        trimBtn: 'Confirm Trim',
        exitTitle: 'Exit Without Saving',
        exitMsg: 'You have an unsaved recorded flight.\n\nDo you really want to close Flight Recorder without saving?',
        exitBtn: 'Exit Without Saving',
        exitBackBtn: 'Back',
        crashTitle: 'Recover Flight Recording',
        crashMsg: 'An auto-save from a previous unexpected close of Flight Recorder was found.\n\nDo you want to restore this recording?',
        crashRestoreBtn: 'Restore Flight',
        crashDiscardBtn: 'Discard',
        saveModalTitle: 'Save Flight Recording',
        saveModalSubtitle: 'Enter a custom title or description to organize your clip in Saved Flights.',
        saveInputLabel: 'Clip Title / Flight Name',
        saveInputPlaceholder: 'E.g.: Crosswind Landing at Congonhas RWY 17R',
        saveInputHint: 'Tip: This title will appear prominently on your saved flight card.',
        savePreviewBadge: 'Clip Preview',
        saveConfirmBtn: 'Save Flight',
        savingToast: 'Saving flight to SavedFlights...'
      },
      hud: {
        closeTitle: 'Close Mini Player',
        dragTitle: 'Click and drag to move window',
        recordStartTitle: 'Start Recording (Record)',
        recordStopTitle: 'Stop Recording (Stop)',
        rewind5: 'Rewind 5 seconds',
        back10: 'Back 10 frames',
        back1: 'Back 1 frame',
        playPause: 'Play / Pause Replay',
        stopReplay: 'Stop Replay (Stop)',
        fwd1: 'Forward 1 frame',
        fwd10: 'Forward 10 frames',
        fwd5: 'Fast forward 5 seconds',
        loop: 'Toggle Loop'
      }
    }
  };

  if (root.i18n && typeof root.i18n.registerLocale === 'function') {
    root.i18n.registerLocale(locale.lang, locale);
  } else {
    root.i18nLocales = root.i18nLocales || {};
    root.i18nLocales[locale.lang] = locale;
  }
})(typeof self !== 'undefined' ? self : this);
