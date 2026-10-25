/**
 * Flight Recorder - Spanish Language Pack
 * Code: 'es'
 */
(function (root) {
  'use strict';

  const locale = {
    lang: 'es',
    name: 'Español',
    flag: '🇪🇸',
    sub: 'España / Latam',
    translations: {
      nav: {
        record: 'Grabación',
        recordTitle: 'Panel de Grabación y Repetición de Vuelo',
        records: 'Vuelos Guardados',
        recordsTitle: 'Lista de Vuelos Guardados',
        telemetry: 'Telemetría',
        telemetryTitle: 'Telemetría en Vivo e Instrumentos de Vuelo',
        settings: 'Configuración',
        settingsTitle: 'Configuración y Atajos de Teclado'
      },
      update: {
        downloading: 'Actualización encontrada. Descargando...',
        ready: 'Actualización lista. Reinicie para aplicar.'
      },
      status: {
        connecting: 'Conectando a Microsoft Flight Simulator...',
        disconnected: 'SimConnect Desconectado — Por favor, inicie MSFS',
        connected: 'Conectado a Microsoft Flight Simulator',
        recording: 'Grabando Vuelo...',
        replaying: 'Reproduciendo Vuelo...',
        paused: 'Repetición Pausada',
        idleSaved: 'Vuelo Guardado',
        idleUnsaved: 'Grabación No Guardada'
      },
      aircraft: {
        label: 'AERONAVE',
        none: 'Ninguna aeronave detectada',
        noRecording: 'Sin grabación activa',
        recordingActive: 'Grabación activa',
        profileStd: 'Perfil Estándar',
        profileCustom: 'Perfil Personalizado',
        profileTooltip: 'Sistema de grabación/repetición de variables personalizadas (SimConnect Nativo - Cero WASM)',
        savedBadge: '✓ Guardado'
      },
      controls: {
        recordFlight: 'Grabar Vuelo',
        stopRecording: 'Detener Grabación',
        replay: 'Replay',
        replayAiTitle: 'Opciones de repetición (Aeronave de formación IA)',
        pause: 'Pausar',
        resume: 'Reanudar',
        stop: 'Detener',
        loop: 'Bucle',
        loopActiveToast: 'Repetición en bucle activada',
        loopInactiveToast: 'Repetición en bucle desactivada',
        speed: 'Velocidad',
        speedToast: 'Velocidad de repetición: x{rate}',
        trim: 'Recortar',
        trimStart: 'Recortar Inicio (desde el cuadro actual)',
        trimEnd: 'Recortar Fin (hasta el cuadro actual)',
        trimStartToast: 'Inicio del vuelo recortado al cuadro actual',
        trimEndToast: 'Fin del vuelo recortado al cuadro actual',
        frame: 'Cuadro',
        noFlight: 'Ningún vuelo grabado o cargado',
        save: 'Guardar en SavedFlights',
        load: 'Cargar desde Vuelos Guardados',
        hudLauncher: 'Barra de Control de Repetición Flotante (HUD)',
        topmostBadge: 'TOPMOST'
      },
      records: {
        title: 'Vuelos Guardados',
        desc: 'Historial y gestión de grabaciones guardadas',
        searchPlaceholder: 'Buscar por aeronave o nombre de archivo...',
        refresh: 'Actualizar',
        refreshToast: 'Lista de vuelos actualizada',
        import: 'Importar',
        openFolder: 'Abrir Carpeta',
        statTotal: 'Total de Vuelos',
        statTime: 'Tiempo Grabado',
        minutesUnit: 'min',
        emptyTitle: 'No se encontraron vuelos guardados',
        emptyDesc: 'Grabe un vuelo en el simulador y haga clic en Guardar para almacenarlo aquí.',
        emptyFilterDesc: 'Ningún resultado coincide con su búsqueda.',
        load: 'Cargar',
        loadTitle: 'Cargar este vuelo para repetición',
        deleteTitle: 'Eliminar grabación',
        confirmDeleteTitle: 'Eliminar Vuelo Guardado',
        confirmDeleteMsg: '¿Está seguro de que desea eliminar permanentemente el vuelo "{name}"? Esta acción no se puede deshacer.',
        confirmDeleteBtn: 'Eliminar Vuelo',
        framesSuffix: 'cuadros',
        flightLoadedToast: '¡Vuelo "{name}" cargado con éxito!'
      },
      telemetry: {
        title: 'Telemetría de Vuelo en Vivo',
        desc: 'Parámetros en tiempo real mediante SimConnect',
        copyCoords: 'Copiar Coords',
        coordsCopiedToast: 'Coordenadas copiadas: {coords}',
        noCoordsToast: 'No hay coordenadas disponibles en este momento',
        valCopiedToast: 'Copiado: {val}',
        pitch: 'Cabeceo (Pitch)',
        bank: 'Alabeo (Bank)',
        heading: 'Rumbo (Heading)',
        gear: 'Tren de Aterrizaje',
        throttle: 'Acelerador (Throttle)',
        landingRate: 'Tasa de Aterrizaje',
        flaps: 'Flaps',
        connectedAircraft: 'AERONAVE CONECTADA',
        contactStatus: 'ESTADO DE CONTACTO',
        latitude: 'Latitud',
        longitude: 'Longitud',
        altitude: 'Altitud',
        gnd: 'TIERRA (GND)',
        air: 'EN VUELO (AIR)',
        gearDown: 'ABAJO',
        gearUp: 'ARRIBA',
        chartPlaceholder: 'El gráfico de telemetría aparecerá cuando haya cuadros grabados'
      },
      settings: {
        title: 'Configuración',
        subtitle: 'Personalice el idioma de visualización, apariencia, atajos de teclado y preferencias de Flight Recorder',
        languageTitle: 'Idioma / Language',
        languageDesc: 'Elija el idioma de visualización para Flight Recorder.',
        languageToast: 'Idioma cambiado a Español',
        themeTitle: 'Apariencia y Tema',
        themeDesc: 'Alterne entre el tema oscuro Flux UI, tema claro moderno o sincronización con Windows.',
        themeDark: 'Oscuro',
        themeLight: 'Claro',
        themeSystem: 'Sistema',
        themeDarkToast: 'Modo Oscuro (Flux UI) activado',
        themeLightToast: 'Modo Claro activado',
        themeSystemToast: 'Tema sincronizado con Windows',
        hotkeysTitle: 'Atajos Globales de Teclado',
        hotkeysDesc: 'Controle la grabación y repetición directamente desde MSFS mediante atajos de teclado.',
        hotkeysEnable: 'Habilitar Atajos Globales',
        hotkeysEnableDesc: 'Los atajos configurados funcionarán directamente en MSFS 2020 / 2024 incluso en pantalla completa.',
        hotkeysDisabledDesc: 'Atajos deshabilitados: el simulador no responderá a las teclas mientras esta opción esté desactivada.',
        hotkeysActive: 'Habilitado',
        hotkeysInactive: 'Deshabilitado',
        hotkeysEnabledToast: '¡Atajos globales habilitados en MSFS!',
        hotkeysDisabledToast: 'Atajos globales deshabilitados.',
        hotkeysCombos: 'Combinaciones de Teclas',
        hotkeysActiveCount: '{count} atajos activos',
        hotkeysInactiveCount: '{count} atajos (deshabilitados)',
        hotkeysConfigureBtn: 'Configurar Atajos',
        folderTitle: 'Directorio de Grabaciones (SavedFlights)',
        folderDesc: 'Carpeta donde se almacenan automáticamente los vuelos al hacer clic en Guardar.',
        folderOpenBtn: 'Abrir en Explorer',
        autoRecordTitle: 'Grabación Automática por Altitud',
        autoRecordDesc: 'Inicia la grabación automáticamente cuando la app está abierta y la aeronave vuela por debajo del umbral de altitud (ideal para aproximaciones y aterrizajes).',
        autoRecordTriggerAlt: 'Altitud de Disparo',
        autoRecordRangeDesc: 'Configurable entre 1,000 y 15,000 ft (por defecto: 6,000 ft). La grabación comienza al descender por debajo de este límite.',
        autoRecordStatusActive: 'Habilitado (< {alt})',
        autoRecordStatusInactive: 'Deshabilitado',
        autoRecordDefault: 'Predeterminado',
        autoRecordEnabledToast: 'Grabación automática por debajo de {alt} ACTIVADA',
        autoRecordDisabledToast: 'Grabación automática por altitud DESACTIVADA',
        autoRecordSetToast: 'Altitud de grabación automática establecida en {alt}',
        stayOnTopTitle: 'Siempre Visible (Stay on Top)',
        stayOnTopDesc: 'Mantiene Flight Recorder anclado por encima de otras ventanas y del simulador.',
        stayOnTopActiveToast: 'Stay on Top: Habilitado',
        stayOnTopInactiveToast: 'Stay on Top: Deshabilitado',
        hudTitle: 'Mini-Reproductor Flotante de Repetición (HUD)',
        hudDesc: 'Opciones de visualización y comportamiento para el controlador flotante en el simulador.',
        hudAutoHide: 'Ocultar automáticamente al pausar',
        hudAutoHideDesc: 'Desvanece automáticamente el mini-reproductor cuando la repetición está pausada o detenida.',
        hudStyle: 'Estilo Visual del HUD',
        hudStyleDesc: 'Elija entre estilo oscuro sólido o translúcido con efecto cristal.',
        hudStyleSolid: 'Oscuro Sólido',
        hudStyleTranslucent: 'Translúcido (Cristal)'
      },
      hotkeys: {
        modalTitle: 'Configurar Atajos Globales',
        modalSubtitle: 'Haga clic en Grabar para capturar la combinación de teclas en su teclado.',
        colAction: 'Acción',
        colCurrent: 'Atajo Actual',
        colCapture: 'Capturar',
        btnRestore: 'Restaurar Predeterminados',
        btnCancel: 'Cancelar',
        btnSave: 'Guardar Atajos',
        btnSaveToast: '✓ ¡Atajos guardados con éxito!',
        recordKey: 'Grabar Tecla',
        recording: 'Presione teclas...',
        savedToast: 'Atajos restablecidos a los valores predeterminados. Haga clic en Guardar para aplicar.',
        cancelledToast: 'Configuración cancelada.',
        shortcutRemovedToast: 'Atajo eliminado para "{name}".',
        shortcutSetToast: 'Atajo configurado: {combo}',
        confirmRestoreTitle: 'Restaurar Atajos Predeterminados',
        confirmRestoreMsg: '¿Desea restaurar todos los atajos a los valores predeterminados de fábrica? Los cambios no guardados se sobrescribirán.',
        confirmRestoreBtn: 'Restaurar Predeterminados',
        Record: {
          name: 'Grabar Vuelo (Record)',
          desc: 'Inicia la grabación de las posiciones de vuelo'
        },
        StopRecording: {
          name: 'Detener Grabación (Stop)',
          desc: 'Detiene la grabación activa'
        },
        Replay: {
          name: 'Reproducir Vuelo (Play)',
          desc: 'Inicia la reproducción del vuelo grabado'
        },
        Pause: {
          name: 'Pausar Repetición (Pause)',
          desc: 'Congela la reproducción del vuelo en el simulador'
        },
        Resume: {
          name: 'Reanudar Repetición (Resume)',
          desc: 'Reanuda la reproducción pausada'
        },
        StopReplay: {
          name: 'Detener Repetición (Stop)',
          desc: 'Finaliza la repetición y devuelve los controles del simulador'
        },
        SaveToDisk: {
          name: 'Guardado Rápido (Quick Save)',
          desc: 'Guarda la grabación directamente en la carpeta SavedFlights'
        },
        ToggleReplayHud: {
          name: 'Barra de Repetición (HUD)',
          desc: 'Muestra u oculta el mini-reproductor flotante de repetición'
        }
      },
      dialog: {
        cancel: 'Cancelar',
        confirm: 'Confirmar',
        attention: 'Atención',
        understood: 'Entendido',
        unsavedTitle: 'Grabación No Guardada',
        unsavedRecordMsg: 'Tiene un vuelo grabado que aún no ha sido guardado.\nIniciar una nueva grabación reemplazará el vuelo actual.\n\n¿Desea continuar?',
        unsavedRecordBtn: 'Descartar y Grabar',
        unsavedLoadMsg: 'Tiene un vuelo grabado que aún no ha sido guardado.\nCargar otro vuelo reemplazará el vuelo actual.\n\n¿Desea continuar?',
        unsavedLoadBtn: 'Descartar y Cargar',
        trimStartTitle: 'Recortar Inicio del Vuelo',
        trimStartMsg: 'Tiene un vuelo grabado no guardado.\nEl recorte eliminará permanentemente todos los cuadros anteriores al punto actual.\n\n¿Desea continuar?',
        trimEndTitle: 'Recortar Fin del Vuelo',
        trimEndMsg: 'Tiene un vuelo grabado no guardado.\nEl recorte eliminará permanentemente todos los cuadros posteriores al punto actual.\n\n¿Desea continuar?',
        trimBtn: 'Confirmar Recorte',
        exitTitle: 'Salir sin Guardar',
        exitMsg: 'Tiene un vuelo grabado que aún no ha sido guardado.\n\n¿Realmente desea cerrar Flight Recorder sin guardar?',
        exitBtn: 'Salir sin Guardar',
        exitBackBtn: 'Volver',
        crashTitle: 'Recuperar Grabación de Vuelo',
        crashMsg: 'Se encontró un guardado automático recuperado de un cierre inesperado anterior de Flight Recorder.\n\n¿Desea restaurar esta grabación?',
        crashRestoreBtn: 'Restaurar Vuelo',
        crashDiscardBtn: 'Descartar'
      },
      hud: {
        closeTitle: 'Cerrar Mini-Reproductor',
        dragTitle: 'Haga clic y arrastre para mover la ventana',
        recordStartTitle: 'Iniciar Grabación (Record)',
        recordStopTitle: 'Detener Grabación (Stop)',
        rewind5: 'Retroceder 5 segundos',
        back10: 'Retroceder 10 cuadros',
        back1: 'Retroceder 1 cuadro',
        playPause: 'Iniciar / Pausar Repetición',
        stopReplay: 'Detener Repetición (Stop)',
        fwd1: 'Avanzar 1 cuadro',
        fwd10: 'Avanzar 10 cuadros',
        fwd5: 'Avanzar 5 segundos',
        loop: 'Alternar Bucle'
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
