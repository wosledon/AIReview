import React, { useEffect, useMemo, useState } from 'react';
import type { Density, ThemeMode } from '../contexts/UISettingsContext';
import { UISettingsContext } from '../contexts/UISettingsContext';

const THEME_KEY = 'ui.theme';
const DENSITY_KEY = 'ui.density';

function applyTheme(theme: ThemeMode) {
  const root = document.documentElement;
  const isDark = theme === 'dark' || (theme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
  root.classList.toggle('dark', isDark);
}

function applyDensity(density: Density) {
  const root = document.documentElement;
  root.classList.remove('density-compact', 'density-comfortable', 'density-full');
  root.classList.add(`density-${density}`);
}

export const UISettingsProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [theme, setThemeState] = useState<ThemeMode>(() => {
    const saved = localStorage.getItem(THEME_KEY) as ThemeMode | null;
    return saved ?? 'system';
  });
  const [density, setDensityState] = useState<Density>(() => {
    const saved = localStorage.getItem(DENSITY_KEY) as Density | null;
    return saved ?? 'comfortable';
  });

  useEffect(() => {
    applyTheme(theme);
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const listener = () => theme === 'system' && applyTheme('system');
    media.addEventListener?.('change', listener);
    return () => media.removeEventListener?.('change', listener);
  }, [theme]);

  useEffect(() => {
    applyDensity(density);
  }, [density]);

  const setTheme = (t: ThemeMode) => {
    setThemeState(t);
    localStorage.setItem(THEME_KEY, t);
  };

  const setDensity = (d: Density) => {
    setDensityState(d);
    localStorage.setItem(DENSITY_KEY, d);
  };

  const value = useMemo(() => ({ theme, setTheme, density, setDensity }), [theme, density]);
  return <UISettingsContext.Provider value={value}>{children}</UISettingsContext.Provider>;
};

export default UISettingsProvider;
