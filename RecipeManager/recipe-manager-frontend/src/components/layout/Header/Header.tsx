// components/layout/header/Header.tsx

import { Logo } from '@/components/ui';
import styles from './Header.module.css';

import { NavLink } from '@/components/ui/NavLink/NavLink.js';

export const Header:React.FC = () => {  
    return (
        <div>
            <div className={styles.headerContainer}>
                <Logo/>
                <div >
                    <NavLink to='/'>Home</NavLink>
                </div>
            </div>
        </div>
    )
}