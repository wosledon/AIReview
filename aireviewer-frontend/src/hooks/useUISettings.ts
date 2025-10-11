import { useContext } from 'react';
import { UISettingsContext, type UISettingsState } from '../contexts/UISettingsContext';

export const useUISettings = () => {
  const ctx = useContext(UISettingsContext) as UISettingsState | undefined;
  if (!ctx) throw new Error('useUISettings must be used within UISettingsProvider');
  return ctx;
};

export default useUISettings;
