/**
 * Flight Recorder - French Language Pack
 * Code: 'fr'
 */
(function (root) {
  'use strict';

  const locale = {
    lang: 'fr',
    name: 'Français',
    flag: '🇫🇷',
    sub: 'France',
    translations: {
      nav: {
        record: 'Enregistrement',
        recordTitle: 'Panneau d\'Enregistrement et Relecture de Vol',
        records: 'Vols Enregistrés',
        recordsTitle: 'Liste des Vols Enregistrés',
        telemetry: 'Télémétrie',
        telemetryTitle: 'Télémétrie en Direct et Instruments de Vol',
        settings: 'Paramètres',
        settingsTitle: 'Paramètres et Raccourcis Clavier'
      },
      update: {
        downloading: 'Mise à jour trouvée. Téléchargement...',
        ready: 'Mise à jour prête. Redémarrez pour appliquer.'
      },
      status: {
        connecting: 'Connexion à Microsoft Flight Simulator...',
        disconnected: 'SimConnect Déconnecté — Veuillez démarrer MSFS',
        connected: 'Connecté à Microsoft Flight Simulator',
        recording: 'Enregistrement du Vol...',
        replaying: 'Lecture du Vol...',
        paused: 'Relecture en Pause',
        idleSaved: 'Vol Enregistré',
        idleUnsaved: 'Enregistrement Non Sauvegardé'
      },
      aircraft: {
        label: 'AÉRONEF',
        none: 'Aucun aéronef détecté',
        noRecording: 'Aucun enregistrement actif',
        recordingActive: 'Enregistrement actif',
        profileStd: 'Profil Standard',
        profileCustom: 'Profil Personnalisé',
        profileTooltip: 'Système d\'enregistrement/relecture de variables personnalisées (SimConnect Natif - Zéro WASM)',
        savedBadge: '✓ Enregistré'
      },
      controls: {
        recordFlight: 'Enregistrer le Vol',
        stopRecording: 'Arrêter l\'Enregistrement',
        replay: 'Replay',
        replayAiTitle: 'Options de relecture (Aéronef de formation IA)',
        pause: 'Pause',
        resume: 'Reprendre',
        stop: 'Arrêter',
        loop: 'Boucle',
        loopActiveToast: 'Relecture en boucle activée',
        loopInactiveToast: 'Relecture en boucle désactivée',
        speed: 'Vitesse',
        speedToast: 'Vitesse de relecture : x{rate}',
        trim: 'Rogner',
        trimStart: 'Rogner le Début (à partir de cette image)',
        trimEnd: 'Rogner la Fin (jusqu\'à cette image)',
        trimStartToast: 'Début du vol rogné à l\'image actuelle',
        trimEndToast: 'Fin du vol rognée à l\'image actuelle',
        frame: 'Image',
        noFlight: 'Aucun vol enregistré ou chargé',
        save: 'Enregistrer dans SavedFlights',
        load: 'Charger depuis les Vols Enregistrés',
        hudLauncher: 'Barre de Contrôle de Relecture Flottante (HUD)',
        topmostBadge: 'TOPMOST'
      },
      records: {
        title: 'Vols Enregistrés',
        desc: 'Historique et gestion des enregistrements de vol',
        searchPlaceholder: 'Rechercher par aéronef ou nom de fichier...',
        refresh: 'Actualiser',
        refreshToast: 'Liste des vols actualisée',
        import: 'Importer',
        openFolder: 'Ouvrir le Dossier',
        statTotal: 'Total des Vols',
        statTime: 'Temps Enregistré',
        minutesUnit: 'min',
        emptyTitle: 'Aucun vol enregistré trouvé',
        emptyDesc: 'Enregistrez un vol dans le simulateur et cliquez sur Enregistrer pour le stocker ici.',
        emptyFilterDesc: 'Aucun résultat ne correspond à votre recherche.',
        load: 'Charger',
        loadTitle: 'Charger ce vol pour la relecture',
        deleteTitle: 'Supprimer l\'enregistrement',
        confirmDeleteTitle: 'Supprimer le Vol Enregistré',
        confirmDeleteMsg: 'Êtes-vous sûr de vouloir supprimer définitivement le vol "{name}" ? Cette action est irréversible.',
        confirmDeleteBtn: 'Supprimer le Vol',
        framesSuffix: 'images',
        flightLoadedToast: 'Vol "{name}" chargé avec succès !'
      },
      telemetry: {
        title: 'Télémétrie de Vol en Direct',
        desc: 'Paramètres en temps réel via SimConnect',
        copyCoords: 'Copier Coords',
        coordsCopiedToast: 'Coordonnées copiées : {coords}',
        noCoordsToast: 'Aucune coordonnée disponible actuellement',
        valCopiedToast: 'Copié : {val}',
        pitch: 'Tangage (Pitch)',
        bank: 'Roulis (Bank)',
        heading: 'Cap (Heading)',
        gear: 'Train d\'Atterrissage',
        throttle: 'Manette des Gaz',
        landingRate: 'Taux de Toucher',
        flaps: 'Volets (Flaps)',
        connectedAircraft: 'AÉRONEF CONNECTÉ',
        contactStatus: 'STATUT DU CONTACT',
        latitude: 'Latitude',
        longitude: 'Longitude',
        altitude: 'Altitude',
        gnd: 'SOL (GND)',
        air: 'EN VOL (AIR)',
        gearDown: 'SORTI',
        gearUp: 'RENTRE',
        chartPlaceholder: 'Le graphique de télémétrie apparaîtra dès que le vol aura des images'
      },
      settings: {
        title: 'Paramètres',
        subtitle: 'Personnalisez la langue d\'affichage, l\'apparence, les raccourcis clavier et les préférences de Flight Recorder',
        languageTitle: 'Langue / Language',
        languageDesc: 'Choisissez la langue d\'affichage de Flight Recorder.',
        languageToast: 'Langue changée en Français',
        themeTitle: 'Apparence et Thème',
        themeDesc: 'Basculez entre le thème sombre Flux UI, le thème clair moderne ou la synchronisation Windows.',
        themeDark: 'Sombre',
        themeLight: 'Clair',
        themeSystem: 'Système',
        themeDarkToast: 'Mode Sombre (Flux UI) activé',
        themeLightToast: 'Mode Clair activé',
        themeSystemToast: 'Thème synchronisé avec Windows',
        hotkeysTitle: 'Raccourcis Clavier Globaux',
        hotkeysDesc: 'Contrôlez l\'enregistrement et la relecture directement depuis MSFS via des raccourcis clavier.',
        hotkeysEnable: 'Activer les Raccourcis Globaux',
        hotkeysEnableDesc: 'Les raccourcis configurés fonctionneront directement dans MSFS 2020 / 2024 même en plein écran.',
        hotkeysDisabledDesc: 'Raccourcis désactivés : le simulateur ne répondra pas aux touches tant que cette option est désactivée.',
        hotkeysActive: 'Activé',
        hotkeysInactive: 'Désactivé',
        hotkeysEnabledToast: 'Raccourcis globaux activés dans MSFS !',
        hotkeysDisabledToast: 'Raccourcis globaux désactivés.',
        hotkeysCombos: 'Combinaisons de Touches',
        hotkeysActiveCount: '{count} raccourcis actifs',
        hotkeysInactiveCount: '{count} raccourcis (désactivés)',
        hotkeysConfigureBtn: 'Configurer les Raccourcis',
        folderTitle: 'Dossier des Enregistrements (SavedFlights)',
        folderDesc: 'Dossier où les vols sont automatiquement stockés en cliquant sur Enregistrer.',
        folderOpenBtn: 'Ouvrir dans l\'Explorateur',
        autoRecordTitle: 'Enregistrement Auto par Altitude',
        autoRecordDesc: 'Démarre automatiquement l\'enregistrement lorsque l\'application est ouverte et que l\'aéronef vole sous le seuil d\'altitude (idéal pour les approches et atterrissages).',
        autoRecordTriggerAlt: 'Altitude de Déclenchement',
        autoRecordRangeDesc: 'Configurable entre 1 000 et 15 000 ft (par défaut : 6 000 ft). L\'enregistrement démarre lors de la descente sous cette limite.',
        autoRecordStatusActive: 'Activé (< {alt})',
        autoRecordStatusInactive: 'Désactivé',
        autoRecordDefault: 'Par défaut',
        autoRecordEnabledToast: 'Enregistrement auto sous {alt} ACTIVÉ',
        autoRecordDisabledToast: 'Enregistrement auto par altitude DÉSACTIVÉ',
        autoRecordSetToast: 'Altitude de déclenchement auto définie sur {alt}',
        stayOnTopTitle: 'Toujours Visible (Stay on Top)',
        stayOnTopDesc: 'Garde Flight Recorder épinglé au-dessus des autres fenêtres et du simulateur.',
        stayOnTopActiveToast: 'Stay on Top : Activé',
        stayOnTopInactiveToast: 'Stay on Top : Désactivé',
        hudTitle: 'Mini-Lecteur Flottant de Relecture (HUD)',
        hudDesc: 'Options d\'affichage et de comportement pour le mini-contrôleur flottant dans le simulateur.',
        hudAutoHide: 'Masquer automatiquement en pause',
        hudAutoHideDesc: 'Estompe automatiquement le mini-lecteur lorsque la relecture est en pause ou arrêtée.',
        hudStyle: 'Style Visuel du HUD',
        hudStyleDesc: 'Choisissez entre un style sombre solide ou translucide avec effet de verre.',
        hudStyleSolid: 'Sombre Solide',
        hudStyleTranslucent: 'Translucide (Verre)'
      },
      hotkeys: {
        modalTitle: 'Configurer les Raccourcis Globaux',
        modalSubtitle: 'Cliquez sur Enregistrer pour capturer la combinaison de touches sur votre clavier.',
        colAction: 'Action',
        colCurrent: 'Raccourci Actuel',
        colCapture: 'Capturer',
        btnRestore: 'Restaurer les Défauts',
        btnCancel: 'Annuler',
        btnSave: 'Enregistrer les Raccourcis',
        btnSaveToast: '✓ Raccourcis enregistrés avec succès !',
        recordKey: 'Enregistrer Touche',
        recording: 'Appuyez sur les touches...',
        savedToast: 'Raccourcis réinitialisés par défaut. Cliquez sur Enregistrer pour appliquer.',
        cancelledToast: 'Configuration annulée.',
        shortcutRemovedToast: 'Raccourci supprimé pour "{name}".',
        shortcutSetToast: 'Raccourci configuré : {combo}',
        confirmRestoreTitle: 'Restaurer les Raccourcis par Défaut',
        confirmRestoreMsg: 'Voulez-vous restaurer tous les raccourcis aux valeurs d\'usine ? Les modifications non enregistrées seront écrasées.',
        confirmRestoreBtn: 'Restaurer les Défauts',
        Record: {
          name: 'Enregistrer le Vol (Record)',
          desc: 'Démarre l\'enregistrement des positions de vol'
        },
        StopRecording: {
          name: 'Arrêter l\'Enregistrement (Stop)',
          desc: 'Arrête l\'enregistrement actif'
        },
        Replay: {
          name: 'Lecture du Vol (Play)',
          desc: 'Démarre la lecture du vol enregistré'
        },
        Pause: {
          name: 'Mettre en Pause (Pause)',
          desc: 'Fige la lecture du vol dans le simulateur'
        },
        Resume: {
          name: 'Reprendre la Lecture (Resume)',
          desc: 'Reprend la lecture en pause'
        },
        StopReplay: {
          name: 'Arrêter la Relecture (Stop)',
          desc: 'Met fin à la relecture et restaure les commandes du simulateur'
        },
        SaveToDisk: {
          name: 'Sauvegarde Rapide (Quick Save)',
          desc: 'Enregistre le vol directement dans le dossier SavedFlights'
        },
        ToggleReplayHud: {
          name: 'Barre de Relecture (HUD)',
          desc: 'Affiche ou masque le mini-lecteur flottant de relecture'
        }
      },
      dialog: {
        cancel: 'Annuler',
        confirm: 'Confirmer',
        attention: 'Attention',
        understood: 'Compris',
        unsavedTitle: 'Enregistrement Non Sauvegardé',
        unsavedRecordMsg: 'Vous avez un vol enregistré non sauvegardé.\nDémarrer un nouvel enregistrement remplacera le vol actuel.\n\nVoulez-vous continuer ?',
        unsavedRecordBtn: 'Ignorer et Enregistrer',
        unsavedLoadMsg: 'Vous avez un vol enregistré non sauvegardé.\nCharger un autre vol remplacera le vol actuel.\n\nVoulez-vous continuer ?',
        unsavedLoadBtn: 'Ignorer et Charger',
        trimStartTitle: 'Rogner le Début du Vol',
        trimStartMsg: 'Vous avez un vol enregistré non sauvegardé.\nLe rognage supprimera définitivement toutes les images antérieures au point actuel.\n\nVoulez-vous continuer ?',
        trimEndTitle: 'Rogner la Fin du Vol',
        trimEndMsg: 'Vous avez un vol enregistré non sauvegardé.\nLe rognage supprimera définitivement toutes les images postérieures au point actuel.\n\nVoulez-vous continuer ?',
        trimBtn: 'Confirmer le Rognage',
        exitTitle: 'Quitter sans Enregistrer',
        exitMsg: 'Vous avez un vol enregistré non sauvegardé.\n\nVoulez-vous vraiment fermer Flight Recorder sans enregistrer ?',
        exitBtn: 'Quitter sans Enregistrer',
        exitBackBtn: 'Retour',
        crashTitle: 'Récupérer l\'Enregistrement du Vol',
        crashMsg: 'Une sauvegarde automatique d\'une fermeture inattendue précédente de Flight Recorder a été trouvée.\n\nVoulez-vous restaurer cet enregistrement ?',
        crashRestoreBtn: 'Restaurer le Vol',
        crashDiscardBtn: 'Ignorer'
      },
      hud: {
        closeTitle: 'Fermer le Mini-Lecteur',
        dragTitle: 'Cliquez et glissez pour déplacer la fenêtre',
        recordStartTitle: 'Démarrer l\'Enregistrement (Record)',
        recordStopTitle: 'Arrêter l\'Enregistrement (Stop)',
        rewind5: 'Reculer de 5 secondes',
        back10: 'Reculer de 10 images',
        back1: 'Reculer de 1 image',
        playPause: 'Lecture / Pause Relecture',
        stopReplay: 'Arrêter la Relecture (Stop)',
        fwd1: 'Avancer de 1 image',
        fwd10: 'Avancer de 10 images',
        fwd5: 'Avancer de 5 secondes',
        loop: 'Basculer la Boucle'
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
