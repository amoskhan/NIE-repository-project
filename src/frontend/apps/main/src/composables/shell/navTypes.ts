// TEMPLATE-OWNED SHELL — do not add project data here.
// This is the shape the staff shell renders. Menu items themselves live in
// src/frontend/apps/main/src/app-config/navigation.ts.
// See .ai/GLOBAL-RULES.md and .ai/FEATURE-app-shell-navigation.md.

export interface NavItem {
  name: string;
  icon: string;
  route: string;
  activeRoutes?: string[];
  permission?: string;
  permissions?: string[];
}
