export type NavigationItem = {
  to: string;
  labelKey: string;
  descriptionKey: string;
  end?: boolean;
};

export const primaryNavigation: NavigationItem[] = [
  {
    to: "/homes",
    labelKey: "sidebar.homes",
    descriptionKey: "sidebar.homesDescription",
  },
];
