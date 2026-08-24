// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Project brand assets used by the staff shell. The brand *label* (product name shown
// in the sidebar/header and the document title) lives in theme/appTheme.ts as
// `brandLabel` and flows through useTheme().brandLabel — change it there. This file
// owns the things the theme config can't express.

import nieLogo from "@/assets/nie-logo.svg";

/** Logo shown in the sidebar, mobile drawer, and compact header. Swap the import to rebrand. */
export const BRAND_LOGO: string = nieLogo;

/**
 * Namespace prefix for the per-page feedback widget's function id
 * (`${FEEDBACK_FUNCTION_PREFIX}.<route-name>`). Set this to your project/module key.
 */
export const FEEDBACK_FUNCTION_PREFIX = "procurement";
