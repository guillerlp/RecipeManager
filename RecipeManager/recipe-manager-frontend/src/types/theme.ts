// src/types/theme.ts

/** What the user chose. Persisted to localStorage under 'theme'. */
export type ThemePreference = 'light' | 'dark' | 'system';

/** What is actually rendered — what lands on <html data-theme>. */
export type Theme = 'light' | 'dark';

export interface ThemeContextType {
    /** The user's choice, including 'system'. */
    preference: ThemePreference;
    /** The resolved palette: 'system' has already been turned into light or dark. */
    theme: Theme;
    setPreference: (preference: ThemePreference) => void;
}
