import { Theme, ThemePreference } from "@/types/theme";
import React, { useCallback, useEffect, useLayoutEffect, useMemo, useState } from "react";
import { ThemeContext } from "./ThemeContext";

const STORAGE_KEY = 'theme';
const DARK_QUERY = '(prefers-color-scheme: dark)';

const isPreference = (value: unknown): value is ThemePreference =>
    value === 'light' || value === 'dark' || value === 'system';

const readPreference = (): ThemePreference => {
    try {
        const saved = window.localStorage.getItem(STORAGE_KEY);
        return isPreference(saved) ? saved : 'system';
    } catch {
        return 'system';
    }
};

const prefersDark = (): boolean => window.matchMedia(DARK_QUERY).matches;

const resolve = (preference: ThemePreference, osPrefersDark: boolean): Theme =>
    preference === 'system' ? (osPrefersDark ? 'dark' : 'light') : preference;

export const ThemeProvider: React.FC<React.PropsWithChildren> = ({ children }) => {
    const isBrowser = typeof window !== 'undefined' && typeof document !== 'undefined';

    const [preference, setPreference] = useState<ThemePreference>(() =>
        isBrowser ? readPreference() : 'system');

    // The OS reading is the only extra state. `theme` itself is derived below, never stored,
    // so preference and the rendered palette cannot drift apart the moment the OS changes.
    const [osPrefersDark, setOsPrefersDark] = useState<boolean>(() => isBrowser && prefersDark());

    const theme = useMemo<Theme>(() => resolve(preference, osPrefersDark), [preference, osPrefersDark]);

    // useLayoutEffect, not useEffect: applies the attribute before the browser paints, so a
    // dark user never sees a light flash even though `theme` is computed rather than initial state.
    useLayoutEffect(() => {
        if (!isBrowser) return;
        document.documentElement.setAttribute('data-theme', theme);
    }, [theme, isBrowser]);

    useEffect(() => {
        if (!isBrowser) return;
        try {
            window.localStorage.setItem(STORAGE_KEY, preference);
        } catch {
            // A browser with storage disabled still themes correctly for this session.
        }
    }, [preference, isBrowser]);

    // Tracks the OS unconditionally — cheap, and it means the reading is fresh the instant the
    // preference moves back to 'system' rather than only while it was already there.
    useEffect(() => {
        if (!isBrowser) return;
        const media = window.matchMedia(DARK_QUERY);
        const onChange = (event: MediaQueryListEvent) => setOsPrefersDark(event.matches);
        media.addEventListener('change', onChange);
        return () => media.removeEventListener('change', onChange);
    }, [isBrowser]);

    // Flips whatever is currently rendered, not the preference itself — so toggling while on
    // 'system' pins an explicit choice rather than fighting the OS on the next change event.
    const toggleTheme = useCallback(
        () => setPreference(theme === 'light' ? 'dark' : 'light'),
        [theme]);

    const contextValue = useMemo(
        () => ({ preference, theme, setPreference, toggleTheme }),
        [preference, theme, setPreference, toggleTheme]);

    return (
        <ThemeContext.Provider value={contextValue}>
            {children}
        </ThemeContext.Provider>
    );
};
