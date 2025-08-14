// src/components/ui/NavLink/NavLink.tsx

import { Link, useLocation } from 'react-router-dom';
import styles from "./NavLink.module.css";

interface NavLinkProps {
  to: string;
  children: React.ReactNode;
}

export const NavLink = ({ to, children }: NavLinkProps) => {
  const location = useLocation();
  const normalize = (p: string) =>
    p.endsWith('/') && p !== '/' ? p.slice(0, -1) : p;
  const current = normalize(location.pathname);
  const target = normalize(to);
  const isActive =
    current === target ||
    (target !== '/' && current.startsWith(`${target}/`));
  
  return (
    <Link 
      to={to} 
      className={`${styles.navLink} ${isActive ? styles.active : ''}`}
      aria-current={isActive ? 'page' : undefined}
      title={typeof children === 'string' ? children : undefined}
    >
      {children}
    </Link>
  );
};