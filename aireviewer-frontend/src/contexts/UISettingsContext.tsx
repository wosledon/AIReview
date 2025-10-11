import { createContext } from 'react';

export type ThemeMode = 'system' | 'light' | 'dark';
export type Density = 'compact' | 'comfortable' | 'full';

export interface UISettingsState {
  theme: ThemeMode;
  setTheme: (t: ThemeMode) => void;
  density: Density;
  setDensity: (d: Density) => void;
}

export const UISettingsContext = createContext<UISettingsState | undefined>(undefined);
