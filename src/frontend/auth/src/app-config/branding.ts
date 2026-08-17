// PROJECT-OWNED — safe to edit. The locked auth login page imports from here.
//
// Brand assets for the auth (login) app. The brand *label* (product name) lives in
// theme/appTheme.ts as `brandLabel` and flows through useTheme().brandLabel — change it
// there. This file owns the logo asset the login page can't express via the theme config.

import appLogo from "../assets/app-logo.svg";

/** Logo shown on every auth screen. Swap the import above to rebrand. */
export const BRAND_LOGO: string = appLogo;
