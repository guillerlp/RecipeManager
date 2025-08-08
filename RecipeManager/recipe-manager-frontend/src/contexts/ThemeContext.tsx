import { ThemeContextType, Theme } from "@/types/theme";
import React, { useCallback, useEffect, useMemo, useState } from "react";

export const ThemeContext = React.createContext<ThemeContextType>({
    theme: 'light',
    toggleTheme: () => {},
    setTheme: () => {}
});

export const ThemeProvider: React.FC<React.PropsWithChildren<{}>> = ({ children }) => {
    const [theme, setTheme] = useState<Theme>(() => {
        const saved = localStorage.getItem('theme');
        return saved === 'dark' ? 'dark' : 'light';
    });

    useEffect(() => {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
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