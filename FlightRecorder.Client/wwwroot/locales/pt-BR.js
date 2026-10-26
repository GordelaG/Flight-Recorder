/**
 * Flight Recorder - Português (Brasil) Language Pack
 * Code: 'pt-BR'
 */
(function (root) {
  'use strict';

  const locale = {
    lang: 'pt-BR',
    name: 'Português',
    flag: '🇧🇷',
    sub: 'Brasil',
    translations: {
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
        themeDark: 'Escuro',
        themeLight: 'Claro',
        themeSystem: 'Sistema',
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
        crashDiscardBtn: 'Descartar',
        saveModalTitle: 'Salvar Gravação de Voo',
        saveModalSubtitle: 'Defina um título ou descrição para organizar seu clipe na lista de voos salvos.',
        saveInputLabel: 'Título do Clipe / Voo',
        saveInputPlaceholder: 'Ex: Pouso Congonhas RWY 17R com vento cruzado',
        saveInputHint: 'Dica: Este título será exibido com destaque no card do voo salvo.',
        savePreviewBadge: 'Prévia do Clipe',
        saveConfirmBtn: 'Salvar Voo',
        savingToast: 'Salvando voo em SavedFlights...'
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

  if (root.i18n && typeof root.i18n.registerLocale === 'function') {
    root.i18n.registerLocale(locale.lang, locale);
  } else {
    root.i18nLocales = root.i18nLocales || {};
    root.i18nLocales[locale.lang] = locale;
  }
})(typeof self !== 'undefined' ? self : this);
