/**
 * Flight Recorder - Internationalization (i18n) System
 * Supports English ('en') as default and Portuguese ('pt-BR').
 * Adding a new language is as simple as adding a key to `translations`.
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.i18n = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  'use strict';

  const translations = {
    en: {
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
        themeDark: 'Dark (Flux UI)',
        themeLight: 'Light (Modern)',
        themeSystem: 'Windows Sync',
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
        crashDiscardBtn: 'Discard'
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
    },
    'pt-BR': {
      nav: {
        record: 'Gravação',
        recordTitle: 'Gravador e Painel de Replay',
        records: 'Voos Salvos',
        recordsTitle: 'Lista de Voos Salvos',
        telemetry: 'Telemetria',
        telemetryTitle: 'Telemetria ao Vivo e Instrumentos de Voo',
        settings: 'Configurações',
        settingsTitle: 'Configurações e Atalhos de Teclado'
      },
      update: {
        downloading: 'Atualização encontrada. Baixando...',
        ready: 'Atualização pronta. Reinicie para aplicar.'
      },
      status: {
        connecting: 'Conectando ao Microsoft Flight Simulator...',
        disconnected: 'SimConnect Desconectado — Por favor, inicie o MSFS',
        connected: 'Conectado ao Microsoft Flight Simulator',
        recording: 'Gravando Voo...',
        replaying: 'Reproduzindo Voo...',
        paused: 'Replay Pausado',
        idleSaved: 'Voo Salvo',
        idleUnsaved: 'Gravação Não Salva'
      },
      aircraft: {
        label: 'AERONAVE',
        none: 'Nenhuma aeronave detectada',
        noRecording: 'Nenhuma gravação ativa',
        recordingActive: 'Gravação ativa',
        profileStd: 'Perfil Standard',
        profileCustom: 'Perfil Customizado',
        profileTooltip: 'Sistema de gravação/replay de variáveis personalizadas (SimConnect Nativo - Zero WASM)',
        savedBadge: '✓ Salvo'
      },
      controls: {
        recordFlight: 'Gravar Voo',
        stopRecording: 'Parar Gravação',
        replay: 'Replay',
        replayAiTitle: 'Opções de replay (Aeronave de formação IA)',
        pause: 'Pausar',
        resume: 'Retomar',
        stop: 'Parar',
        loop: 'Repetir',
        loopActiveToast: 'Loop ativado',
        loopInactiveToast: 'Loop desativado',
        speed: 'Velocidade',
        speedToast: 'Velocidade de Replay: x{rate}',
        trim: 'Cortar',
        trimStart: 'Cortar Início (deste frame em diante)',
        trimEnd: 'Cortar Fim (até este frame)',
        trimStartToast: 'Início do voo cortado até este frame',
        trimEndToast: 'Fim do voo cortado até este frame',
        frame: 'Quadro',
        noFlight: 'Nenhum voo gravado ou carregado',
        save: 'Salvar em SavedFlights',
        load: 'Carregar de Voos Salvos',
        hudLauncher: 'Barra de Controle de Replay Flutuante (HUD)',
        topmostBadge: 'TOPMOST'
      },
      records: {
        title: 'Voos Gravados',
        desc: 'Histórico e gerenciamento de gravações salvas',
        searchPlaceholder: 'Pesquisar por aeronave ou nome do arquivo...',
        refresh: 'Atualizar',
        refreshToast: 'Lista de voos atualizada',
        import: 'Importar',
        openFolder: 'Abrir Pasta',
        statTotal: 'Total de Voos',
        statTime: 'Tempo Gravado',
        minutesUnit: 'min',
        emptyTitle: 'Nenhum voo salvo encontrado',
        emptyDesc: 'Grave um voo no simulador e clique no botão Save para salvá-lo aqui.',
        emptyFilterDesc: 'Nenhum resultado corresponde à sua pesquisa.',
        load: 'Carregar',
        loadTitle: 'Carregar este voo para Replay',
        deleteTitle: 'Excluir gravação',
        confirmDeleteTitle: 'Excluir Voo Gravado',
        confirmDeleteMsg: 'Deseja realmente excluir permanentemente o voo "{name}"? Esta ação não pode ser desfeita.',
        confirmDeleteBtn: 'Excluir Voo',
        framesSuffix: 'frames',
        flightLoadedToast: 'Voo "{name}" carregado com sucesso!'
      },
      telemetry: {
        title: 'Telemetria de Voo ao Vivo',
        desc: 'Parâmetros em tempo real via SimConnect',
        copyCoords: 'Copiar Coords',
        coordsCopiedToast: 'Coordenadas copiadas: {coords}',
        noCoordsToast: 'Nenhuma coordenada disponível no momento',
        valCopiedToast: 'Copiado: {val}',
        pitch: 'Arfagem (Pitch)',
        bank: 'Inclinação (Bank)',
        heading: 'Proa (Heading)',
        gear: 'Trem de Pouso',
        throttle: 'Manete (Throttle)',
        landingRate: 'Razão de Toque',
        flaps: 'Flaps',
        connectedAircraft: 'AERONAVE CONECTADA',
        contactStatus: 'STATUS DE CONTATO',
        latitude: 'Latitude',
        longitude: 'Longitude',
        altitude: 'Altitude',
        gnd: 'SOLO (GND)',
        air: 'EM VOO (AIR)',
        gearDown: 'BAIXADO',
        gearUp: 'RECOLHIDO',
        chartPlaceholder: 'O gráfico de telemetria aparecerá assim que houver quadros gravados'
      },
      settings: {
        title: 'Configurações',
        subtitle: 'Personalize o idioma de exibição, aparência, atalhos do teclado e preferências do Flight Recorder',
        languageTitle: 'Idioma / Language',
        languageDesc: 'Selecione o idioma de exibição da interface do Flight Recorder.',
        languageToast: 'Idioma alterado para Português (Brasil)',
        themeTitle: 'Aparência e Tema',
        themeDesc: 'Alterne entre o tema escuro do Flux UI, tema claro moderno ou sincronizado com o Windows.',
        themeDark: 'Modo Escuro (Flux UI)',
        themeLight: 'Modo Claro (Moderno)',
        themeSystem: 'Sincronizado com o Windows',
        themeDarkToast: 'Modo Escuro (Flux UI) ativado',
        themeLightToast: 'Modo Claro ativado',
        themeSystemToast: 'Tema sincronizado com o Windows',
        hotkeysTitle: 'Atalhos Globais do Teclado',
        hotkeysDesc: 'Controle a gravação e replay diretamente do MSFS via atalhos de teclado.',
        hotkeysEnable: 'Habilitar Atalhos Globais',
        hotkeysEnableDesc: 'As teclas configuradas funcionarão diretamente no MSFS 2020 / 2024 mesmo em tela cheia.',
        hotkeysDisabledDesc: 'Atalhos desativados: o simulador não responderá às teclas enquanto esta opção estiver desligada.',
        hotkeysActive: 'Ativado',
        hotkeysInactive: 'Desativado',
        hotkeysEnabledToast: 'Atalhos globais ativados no MSFS!',
        hotkeysDisabledToast: 'Atalhos globais desativados.',
        hotkeysCombos: 'Combinações de Teclas',
        hotkeysActiveCount: '{count} atalhos ativos',
        hotkeysInactiveCount: '{count} atalhos (inativos)',
        hotkeysConfigureBtn: 'Configurar Atalhos',
        folderTitle: 'Diretório de Gravações (SavedFlights)',
        folderDesc: 'Pasta onde os voos são gravados automaticamente ao clicar em Salvar.',
        folderOpenBtn: 'Abrir no Explorer',
        autoRecordTitle: 'Gravação Automática por Altitude',
        autoRecordDesc: 'Inicia a gravação automaticamente assim que o aplicativo estiver aberto e a aeronave estiver voando abaixo da altitude limite (ideal para registrar aproximações e pousos).',
        autoRecordTriggerAlt: 'Altitude de Disparo',
        autoRecordRangeDesc: 'Configurável entre 1.000 e 15.000 pés (padrão: 6.000 ft). A gravação iniciará assim que cruzar esta altitude em descida.',
        autoRecordStatusActive: 'Ativado (< {alt})',
        autoRecordStatusInactive: 'Desativado',
        autoRecordDefault: 'Padrão',
        autoRecordEnabledToast: 'Gravação automática abaixo de {alt} ATIVADA',
        autoRecordDisabledToast: 'Gravação automática por altitude DESATIVADA',
        autoRecordSetToast: 'Altitude de gravação automática definida para {alt}',
        stayOnTopTitle: 'Sempre Visível (Stay on Top)',
        stayOnTopDesc: 'Mantém o Flight Recorder fixado por cima de outras janelas e do simulador.',
        stayOnTopActiveToast: 'Stay on Top: Ativado',
        stayOnTopInactiveToast: 'Stay on Top: Desativado',
        hudTitle: 'Barra Flutuante de Replay (HUD)',
        hudDesc: 'Opções do mini-player flutuante sobre o simulador.',
        hudAutoHide: 'Ocultar automaticamente ao pausar',
        hudAutoHideDesc: 'Desaparece suavemente a barra flutuante quando o replay estiver pausado ou parado.',
        hudStyle: 'Estilo Visual do HUD',
        hudStyleDesc: 'Escolha entre o estilo escuro sólido ou translúcido com efeito de vidro (blur).',
        hudStyleSolid: 'Escuro Sólido',
        hudStyleTranslucent: 'Translúcido (Vidro)'
      },
      hotkeys: {
        modalTitle: 'Configurar Atalhos Globais',
        modalSubtitle: 'Clique em Gravar para capturar a combinação de teclas no teclado.',
        colAction: 'Ação',
        colCurrent: 'Atalho Atual',
        colCapture: 'Captura',
        btnRestore: 'Restaurar Padrões',
        btnCancel: 'Cancelar',
        btnSave: 'Salvar Atalhos',
        btnSaveToast: '✓ Atalhos salvos com sucesso!',
        recordKey: 'Gravar Tecla',
        recording: 'Pressione as teclas...',
        savedToast: 'Atalhos redefinidos para o padrão. Clique em Salvar para aplicar.',
        cancelledToast: 'Configuração cancelada.',
        shortcutRemovedToast: 'Atalho removido para "{name}".',
        shortcutSetToast: 'Atalho configurado: {combo}',
        confirmRestoreTitle: 'Restaurar Atalhos Padrão',
        confirmRestoreMsg: 'Deseja restaurar todos os atalhos para os valores padrão de fábrica? As alterações não salvas serão substituídas.',
        confirmRestoreBtn: 'Restaurar Padrões',
        Record: {
          name: 'Gravar Voo (Record)',
          desc: 'Inicia a gravação das posições do voo'
        },
        StopRecording: {
          name: 'Parar Gravação (Stop)',
          desc: 'Interrompe a gravação atual'
        },
        Replay: {
          name: 'Replay do Voo (Play)',
          desc: 'Inicia a reprodução do voo gravado'
        },
        Pause: {
          name: 'Pausar Replay (Pause)',
          desc: 'Congela a reprodução do voo no simulador'
        },
        Resume: {
          name: 'Continuar Replay (Resume)',
          desc: 'Retoma a reprodução pausada'
        },
        StopReplay: {
          name: 'Parar Replay (Stop)',
          desc: 'Encerra o replay e devolve os controles do simulador'
        },
        SaveToDisk: {
          name: 'Salvar Voo Rápido (Quick Save)',
          desc: 'Salva o voo gravado diretamente na pasta SavedFlights'
        },
        ToggleReplayHud: {
          name: 'Barra de Replay (HUD)',
          desc: 'Abre ou fecha o popup flutuante da barra de controle de replay'
        }
      },
      dialog: {
        cancel: 'Cancelar',
        confirm: 'Confirmar',
        attention: 'Atenção',
        understood: 'Entendi',
        unsavedTitle: 'Gravação Não Salva',
        unsavedRecordMsg: 'Você possui um voo gravado que ainda não foi salvo.\nIniciar uma nova gravação substituirá o voo atual.\n\nDeseja continuar?',
        unsavedRecordBtn: 'Descartar e Gravar',
        unsavedLoadMsg: 'Você possui um voo gravado que ainda não foi salvo.\nCarregar outra gravação substituirá o voo atual.\n\nDeseja continuar?',
        unsavedLoadBtn: 'Descartar e Carregar',
        trimStartTitle: 'Cortar Início do Voo',
        trimStartMsg: 'Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros anteriores ao ponto atual.\n\nDeseja continuar?',
        trimEndTitle: 'Cortar Fim do Voo',
        trimEndMsg: 'Você possui um voo gravado não salvo.\nO corte removerá permanentemente todos os quadros posteriores ao ponto atual.\n\nDeseja continuar?',
        trimBtn: 'Confirmar Corte',
        exitTitle: 'Sair sem Salvar',
        exitMsg: 'Você possui um voo gravado que ainda não foi salvo.\n\nDeseja realmente fechar o Flight Recorder sem salvar a gravação?',
        exitBtn: 'Sair sem Salvar',
        exitBackBtn: 'Voltar',
        crashTitle: 'Recuperar Gravação de Voo',
        crashMsg: 'Foi encontrado um salvamento automático recuperado de um fechamento anterior do Flight Recorder.\n\nDeseja restaurar esta gravação?',
        crashRestoreBtn: 'Restaurar Voo',
        crashDiscardBtn: 'Descartar'
      },
      hud: {
        closeTitle: 'Fechar Mini Player',
        dragTitle: 'Clique e arraste para mover a janela',
        recordStartTitle: 'Iniciar Gravação (Record)',
        recordStopTitle: 'Parar Gravação (Stop)',
        rewind5: 'Retroceder 5 segundos',
        back10: 'Voltar 10 frames',
        back1: 'Voltar 1 frame',
        playPause: 'Iniciar / Pausar Replay',
        stopReplay: 'Parar Replay (Stop)',
        fwd1: 'Avançar 1 frame',
        fwd10: 'Avançar 10 frames',
        fwd5: 'Avançar 5 segundos',
        loop: 'Alternar Repetição'
      }
    }
  };

  const listeners = new Set();
  const defaultLanguage = 'en';
  let currentLanguage = defaultLanguage;

  try {
    const saved = localStorage.getItem('flightrecorder_lang');
    if (saved && translations[saved]) {
      currentLanguage = saved;
    }
  } catch (e) {}

  function getNestedValue(obj, keyPath) {
    if (!obj || !keyPath) return undefined;
    const parts = keyPath.split('.');
    let cur = obj;
    for (let i = 0; i < parts.length; i++) {
      if (cur == null || typeof cur !== 'object') return undefined;
      cur = cur[parts[i]];
    }
    return cur;
  }

  function t(key, params) {
    let val = getNestedValue(translations[currentLanguage], key);
    if (val === undefined) {
      val = getNestedValue(translations[defaultLanguage], key);
    }
    if (val === undefined) {
      return key;
    }
    if (typeof val === 'string' && params && typeof params === 'object') {
      return val.replace(/\{(\w+)\}/g, (match, pName) => {
        return params[pName] !== undefined ? params[pName] : match;
      });
    }
    return val;
  }

  function applyDOM(rootElement = document) {
    if (!rootElement) return;

    // Elements with text content
    rootElement.querySelectorAll('[data-i18n]').forEach((el) => {
      const key = el.getAttribute('data-i18n');
      if (key) {
        el.textContent = t(key);
      }
    });

    // Elements with HTML content
    rootElement.querySelectorAll('[data-i18n-html]').forEach((el) => {
      const key = el.getAttribute('data-i18n-html');
      if (key) {
        el.innerHTML = t(key);
      }
    });

    // Elements with title attribute
    rootElement.querySelectorAll('[data-i18n-title]').forEach((el) => {
      const key = el.getAttribute('data-i18n-title');
      if (key) {
        el.setAttribute('title', t(key));
      }
    });

    // Elements with placeholder attribute
    rootElement.querySelectorAll('[data-i18n-placeholder]').forEach((el) => {
      const key = el.getAttribute('data-i18n-placeholder');
      if (key) {
        el.setAttribute('placeholder', t(key));
      }
    });

    // Elements with aria-label attribute
    rootElement.querySelectorAll('[data-i18n-aria]').forEach((el) => {
      const key = el.getAttribute('data-i18n-aria');
      if (key) {
        el.setAttribute('aria-label', t(key));
      }
    });

    if (document.documentElement) {
      document.documentElement.lang = currentLanguage;
    }
  }

  function setLanguage(lang, notify = true) {
    if (!translations[lang]) {
      console.warn(`[i18n] Language "${lang}" not found, falling back to ${defaultLanguage}`);
      lang = defaultLanguage;
    }
    currentLanguage = lang;
    try {
      localStorage.setItem('flightrecorder_lang', lang);
    } catch (e) {}

    applyDOM();

    if (notify) {
      listeners.forEach((callback) => {
        try {
          callback(currentLanguage);
        } catch (err) {
          console.error('[i18n] Listener error:', err);
        }
      });
    }
  }

  function onLanguageChange(callback) {
    if (typeof callback === 'function') {
      listeners.add(callback);
    }
    return () => listeners.delete(callback);
  }

  function getLanguage() {
    return currentLanguage;
  }

  function getLanguages() {
    return Object.keys(translations);
  }

  return {
    t,
    setLanguage,
    getLanguage,
    getLanguages,
    applyDOM,
    onLanguageChange,
    translations,
    defaultLanguage
  };
});
