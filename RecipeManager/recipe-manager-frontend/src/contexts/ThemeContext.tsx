import { ThemeContextType, Theme } from "@/types/theme";
import React, { useCallback, useEffect, useMemo, useState } from "react";

export const ThemeContext = React.createContext<ThemeContextType>({
    theme: 'light',
    toggleTheme: () => {},
    setTheme: () => {}
});

export const ThemeProvider: React.FC<React.PropsWithChildren<{}>> = ({ children }) => {
    const isBrowser = typeof window !== 'undefined' && typeof document !== 'undefined';

    const [theme, setTheme] = useState<Theme>(() => {
        if (!isBrowser) return 'light';
        try {
            const saved = window.localStorage.getItem('theme');
            const initial = saved === 'dark' ? 'dark' : 'light';
            if (typeof document !== 'undefined') {
                document.documentElement.setAttribute('data-theme', initial);
            }
            return initial;
        } catch {
            return 'light';
        }
    });

    useEffect(() => {
        if (!isBrowser) return;
        document.documentElement.setAttribute('data-theme', theme);
        try {
            window.localStorage.setItem('theme', theme);
        } catch {
        }
    }, [theme]);

    const toggleTheme = useCallback(() => {
        setTheme(prev => prev === 'light' ? 'dark' : 'light');
    }, []);

    const handleSetTheme = useCallback((newTheme: Theme) => {
        setTheme(newTheme);
    }, []);

    const contextValue = useMemo(() => ({
        theme,
        toggleTheme,
        setTheme: handleSetTheme
    }), [theme, toggleTheme, handleSetTheme]);

    return (
        <ThemeContext.Provider value={contextValue}>
            {children}
        </ThemeContext.Provider>
    );
};