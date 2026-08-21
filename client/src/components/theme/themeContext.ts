import { createContext, useContext } from 'react';

export type Theme = 'light' | 'dark';

export type ThemeContextValue = {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggle: () => void;
};

export const ThemeContext = createContext<ThemeContextValue | null>(null);

export const useTheme = () => {
  const value = useContext(ThemeContext);

  if (!value) {
    throw new Error('useTheme must be used inside <ThemeProvider>');
  }

  return value;
};
