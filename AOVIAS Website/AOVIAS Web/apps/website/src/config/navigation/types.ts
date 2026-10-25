export type NavIconName =
  | 'box'
  | 'layers'
  | 'shield'
  | 'book'
  | 'building'
  | 'users'
  | 'file'
  | 'external'
  | 'search'
  | 'arrowRight';

export type NavItem = {
  id: string;
  label: string;
  href: string;
  description?: string;
  icon?: NavIconName;
  external?: boolean;
  hidden?: boolean;
  children?: readonly NavItem[];
};

export type NavGroup = {
  id: string;
  label: string;
  description?: string;
  items: readonly NavItem[];
};

export type MegaMenuCta = {
  id: string;
  label: string;
  href: string;
  description?: string;
};

export type MegaMenuColumn = {
  id: string;
  title: string;
  items: readonly NavItem[];
  /**
   * Item arrangement within the column.
   * `horizontal` renders a full-width row band (used for Technology → Ecosystem).
   */
  orientation?: 'vertical' | 'horizontal';
};

export type MegaMenuConfig = {
  id: string;
  columns: readonly MegaMenuColumn[];
  cta?: MegaMenuCta;
};

export type MainNavItem = NavItem & {
  megaMenu?: MegaMenuConfig;
};

export type FooterColumn = {
  id: string;
  title: string;
  items: readonly NavItem[];
};

export type UtilityItem = NavItem & {
  /** Soft-hide until a portal/feature is ready. */
  visibility?: 'visible' | 'future';
};

export type SocialLink = {
  id: string;
  label: string;
  href: string;
  icon?: NavIconName;
};
