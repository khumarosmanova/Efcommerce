import React, { createContext, useContext, useState, useEffect } from 'react';
import { translations } from '../translations/translations';

const LanguageContext = createContext({
  language: 'az',
  setLanguage: () => {},
  t: (key) => key,
  languages: [],
});

export const languagesList = [
  { code: 'az', label: 'Azərbaycan', flag: '🇦🇿' },
  { code: 'en', label: 'English', flag: '🇬🇧' },
  { code: 'ru', label: 'Русский', flag: '🇷🇺' },
];

export const LanguageProvider = ({ children }) => {
  const [language, setLanguageState] = useState(() => {
    return localStorage.getItem('shophub_language') || 'az';
  });

  const setLanguage = (lang) => {
    if (translations[lang]) {
      setLanguageState(lang);
      localStorage.setItem('shophub_language', lang);
    }
  };

  const t = (key) => {
    const currentDict = translations[language] || translations.az;
    return currentDict[key] || translations.az[key] || key;
  };

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, languages: languagesList }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useLanguage = () => useContext(LanguageContext);
export const useTranslation = () => {
  const { t, language, setLanguage, languages } = useContext(LanguageContext);
  return { t, language, setLanguage, languages };
};
