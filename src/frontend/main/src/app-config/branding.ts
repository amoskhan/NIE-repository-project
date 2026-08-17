// PROJECT-OWNED — safe to edit. The locked shell imports from here.
//
// Project brand assets used by the staff shell. The brand *label* (product name shown
// in the sidebar/header and the document title) lives in theme/appTheme.ts as
// `brandLabel` and flows through useTheme().brandLabel — change it there. This file
// owns the things the theme config can't express.

import appLogo from "@/assets/app-logo.svg";

/** Logo shown in the sidebar, mobile drawer, and compact header. Swap the import to rebrand. */
export const BRAND_LOGO: string = appLogo;

/**
 * Namespace prefix for the per-page feedback widget's function id
 * (`${FEEDBACK_FUNCTION_PREFIX}.<route-name>`), which is how feedback gets grouped by
 * area in the admin views.
 *
 * It is "procurement" because the bundled sample domain is a procurement app. CHANGE IT
 * to your own project/module key (e.g. "booking", "helpdesk") when you replace the
 * sample — otherwise every piece of feedback in your app is filed under "procurement".
 */
export const FEEDBACK_FUNCTION_PREFIX = "procurement";
