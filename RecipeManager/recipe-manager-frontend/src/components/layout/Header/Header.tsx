// components/layout/header/Header.tsx

import { Logo } from '@/components/ui';
import styles from './Header.module.css';
import { NavLink } from '@/components/ui/NavLink/NavLink.js';

export const Header:React.FC = () => {  
    return (
        <div>
            <div className={styles.logoDiv}>
                <Logo/>
                <div className={styles.navLinkDiv}>
                    <NavLink to='/'>Home</NavLink>
                    <NavLink to='/profile'>Profile</NavLink>
                </div>
            </div>
        </div>
    )
}