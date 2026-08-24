export interface NieTabItem<T extends string = string> {
  id: T;
  label: string;
  icon?: string;
  count?: string | number;
  disabled?: boolean;
  panelId?: string;
}
