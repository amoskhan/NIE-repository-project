// TEMPLATE-OWNED SHELL — do not add project data here.
// This is the shape the staff shell renders. Menu items themselves live in
// src/frontend/main/src/app-config/navigation.ts.
// See .ai/common/11-customization-boundary.md. Changing this file requires a template task.

export interface NavItem {
  name: string;
  icon: string;
  route: string;
  activeRoutes?: string[];
  permission?: string;
  permissions?: string[];
}
