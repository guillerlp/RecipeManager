// src/contexts/ThemeContext.ts
import { ThemeContextType } from "@/types/theme";
import { createContext } from "react";

// No default value: `useTheme` throws when the context is undefined, which is what turns
// "used outside ThemeProvider" from a silently wrong theme into a loud error.
export const ThemeContext = createContext<ThemeContextType | undefined>(undefined);
