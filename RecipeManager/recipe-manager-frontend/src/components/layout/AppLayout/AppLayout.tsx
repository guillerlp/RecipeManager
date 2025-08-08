import { Divider } from "@mui/material";
import styles from "./AppLayout.module.css"
import { Header } from "@/components";

interface AppLayoutProps {
    children: React.ReactNode;
}

export const AppLayout = ({ children }: AppLayoutProps) => {
    return (
        <div className={styles.appWrapper}>
            <Header />
            <Divider />
            <main>
                {children}
            </main>
            <Divider />
        </div>
    )
} 