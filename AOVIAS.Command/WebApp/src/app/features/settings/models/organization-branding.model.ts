export interface BrandingColorsModel {
  primary: string;
  secondary: string;
  accent: string;
}

export interface BrandingReportModel {
  header: string;
  footer: string;
  motto: string;
}

export interface BrandingEmailModel {
  header: string;
  footer: string;
  signature: string;
}

export interface BrandingLoginModel {
  welcomeMessage: string;
  tagline: string;
}

export interface BrandingConfigModel {
  colors: BrandingColorsModel;
  report: BrandingReportModel;
  email: BrandingEmailModel;
  login: BrandingLoginModel;
}

export interface BrandingAssetUrlsModel {
  primaryLogoUrl: string | null;
  secondaryLogoUrl: string | null;
  faviconUrl: string | null;
  reportLogoUrl: string | null;
  emailHeaderLogoUrl: string | null;
  emailFooterLogoUrl: string | null;
  loginBackgroundUrl: string | null;
}

export interface OrganizationBrandingModel {
  organizationId: string;
  canEdit: boolean;
  assets: BrandingAssetUrlsModel;
  config: BrandingConfigModel;
  rowVersion: string | null;
}

export type BrandingAssetKey =
  | 'secondary'
  | 'favicon'
  | 'report'
  | 'emailHeader'
  | 'emailFooter'
  | 'loginBackground';

export const BRANDING_ASSET_LABELS: Record<BrandingAssetKey | 'primary', string> = {
  primary: 'Primary Logo',
  secondary: 'Secondary Logo',
  favicon: 'Favicon',
  report: 'Report Logo',
  emailHeader: 'Email Header Logo',
  emailFooter: 'Email Footer Logo',
  loginBackground: 'Login Background Image',
};

export const BRANDING_IMAGE_ACCEPT = '.png,.jpg,.jpeg,.svg,image/png,image/jpeg,image/svg+xml';
