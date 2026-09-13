// src/components/ui/Icon/Icon.tsx
//
// Path data is from Google's Material Icons (Apache License 2.0), copied unchanged from
// @mui/icons-material 7.3.1 (MIT). Names keep the Material Icons catalogue names so a
// glyph can be traced back to its source. Adding one: copy the <path> from that catalogue.

import styles from './Icon.module.css';

interface IconProps {
  className?: string;
}

interface SvgIconProps extends IconProps {
  children: React.ReactNode;
}

// Decorative only: every current icon sits next to text or inside a labelled control,
// so it is hidden from assistive technology, as MUI's SvgIcon did by default.
const SvgIcon = ({ className, children }: SvgIconProps) => (
  <svg
    className={className ? `${styles.icon} ${className}` : styles.icon}
    viewBox="0 0 24 24"
    aria-hidden="true"
    focusable="false"
  >
    {children}
  </svg>
);

export const SearchIcon = ({ className }: IconProps) => (
  <SvgIcon className={className}>
    <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14" />
  </SvgIcon>
);

export const SunnyIcon = ({ className }: IconProps) => (
  <SvgIcon className={className}>
    <path d="M11 4V2c0-.55.45-1 1-1s1 .45 1 1v2c0 .55-.45 1-1 1s-1-.45-1-1m7.36 3.05 1.41-1.42c.39-.39.39-1.02 0-1.41a.996.996 0 0 0-1.41 0l-1.41 1.42c-.39.39-.39 1.02 0 1.41s1.02.39 1.41 0M22 11h-2c-.55 0-1 .45-1 1s.45 1 1 1h2c.55 0 1-.45 1-1s-.45-1-1-1m-10 8c-.55 0-1 .45-1 1v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1M5.64 7.05 4.22 5.64c-.39-.39-.39-1.03 0-1.41s1.03-.39 1.41 0l1.41 1.41c.39.39.39 1.03 0 1.41s-1.02.39-1.4 0m11.31 9.9c-.39.39-.39 1.03 0 1.41l1.41 1.41c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.41-1.41c-.38-.39-1.02-.39-1.41 0M2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1m3.64 6.78 1.41-1.41c.39-.39.39-1.03 0-1.41s-1.03-.39-1.41 0l-1.41 1.41c-.39.39-.39 1.03 0 1.41.38.39 1.02.39 1.41 0M12 6c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6" />
  </SvgIcon>
);

export const BedtimeIcon = ({ className }: IconProps) => (
  <SvgIcon className={className}>
    <path d="M12.34 2.02C6.59 1.82 2 6.42 2 12c0 5.52 4.48 10 10 10 3.71 0 6.93-2.02 8.66-5.02-7.51-.25-12.09-8.43-8.32-14.96" />
  </SvgIcon>
);

export const BlenderOutlinedIcon = ({ className }: IconProps) => (
  <SvgIcon className={className}>
    <path d="M16.13 15.13 18 3h-4V2h-4v1H5c-1.1 0-2 .9-2 2v4c0 1.1.9 2 2 2h2.23l.64 4.13C6.74 16.05 6 17.43 6 19v1c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2v-1c0-1.57-.74-2.95-1.87-3.87M5 9V5h1.31l.62 4zm10.67-4-1.38 9H9.72L8.33 5zM16 20H8v-1c0-1.65 1.35-3 3-3h2c1.65 0 3 1.35 3 3z" />
    <circle cx="12" cy="18" r="1" />
  </SvgIcon>
);
