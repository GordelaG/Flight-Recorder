/**
 * Flight Recorder - Internationalization (i18n) Engine
 * Modular Architecture: Locales are loaded from individual files in `wwwroot/locales/*.js`
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.i18n = factory();
    // If any locales were loaded before i18n was initialized, register them now
    if (root.i18nLocales && typeof root.i18nLocales === 'object') {
      Object.keys(root.i18nLocales).forEach((lang) => {
        root.i18n.registerLocale(lang, root.i18nLocales[lang]);
      });
    }
  }
})(typeof self !== 'undefined' ? self : this, function () {
  'use strict';

  const locales = {};
  const listeners = new Set();
  const defaultLanguage = 'en';
  let currentLanguage = defaultLanguage;

  try {
    const saved = localStorage.getItem('flightrecorder_lang');
    if (saved) {
      currentLanguage = saved;
    }
  } catch (e) {}

  function registerLocale(lang, data) {
    if (!lang || !data) return;
    locales[lang] = {
      lang: data.lang || lang,
      name: data.name || lang,
      flag: data.flag || '🌐',
      sub: data.sub || '',
      translations: data.translations || data
    };

    // If currently on this language, re-apply DOM
    if (currentLanguage === lang) {
      applyDOM();
    }
  }

  function getAvailableLocales() {
    return Object.keys(locales).map((k) => ({
      lang: locales[k].lang,
      name: locales[k].name,
      flag: locales[k].flag,
      sub: locales[k].sub
    }));
  }

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
    const curObj = locales[currentLanguage]?.translations;
    const defObj = locales[defaultLanguage]?.translations;

    let val = getNestedValue(curObj, key);
    if (val === undefined) {
      val = getNestedValue(defObj, key);
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
    if (!locales[lang] && lang !== defaultLanguage) {
      console.warn(`[i18n] Language "${lang}" not yet loaded, setting anyway`);
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
    return Object.keys(locales);
  }

  return {
    t,
    registerLocale,
    getAvailableLocales,
    setLanguage,
    getLanguage,
    getLanguages,
    applyDOM,
    onLanguageChange,
    locales,
    defaultLanguage
  };
});
