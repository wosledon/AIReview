import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import en from './locales/en.json';
import zh from './locales/zh.json';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      zh: { translation: zh },
    },
    fallbackLng: 'en',
    supportedLngs: ['en', 'zh'],
    interpolation: {
      escapeValue: false,
    },
    detection: {
      // order & from where user language should be detected
      order: ['querystring', 'localStorage', 'navigator'],
      // keys or params to lookup language from
      lookupQuerystring: 'lang',
      // cache user language
      caches: ['localStorage'],
    },
  });

export default i18n;
