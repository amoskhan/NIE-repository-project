import { ref, computed } from "vue";
import type { Component } from "vue";
import { useRouter } from "vue-router";

export interface NavigationItem {
  name: string;
  icon?: Component | string;
  id: string;
  path?: string;
  iconName?: string;
  subtitle?: string;
  requiredRoles?: number[];
  requiredAccessFunctions?: string[];
}

export type ScreenSize = "desktop" | "laptop" | "tablet" | "mobile";

// Global state (Singleton pattern)
const hoveredItem = ref<string | null>(null);
const showMenu = ref(false);
const isExpanded = ref(false);
const isMobileMenuOpen = ref(false);
const isScrolled = ref(false);
const screenSize = ref<ScreenSize>("desktop");
const windowWidth = ref(typeof window !== "undefined" ? window.innerWidth : 0);

export function useSidebar() {
  const router = useRouter();

  // Computed properties for responsive behavior
  const isDesktop = computed(() => windowWidth.value >= 1200);
  const isLaptop = computed(
    () => windowWidth.value >= 1024 && windowWidth.value < 1200,
  );
  const isTablet = computed(
    () => windowWidth.value >= 768 && windowWidth.value < 1024,
  );
  const isMobile = computed(() => windowWidth.value < 768);

  const sidebarWidth = computed(() => {
    if (isMobile.value) return "280px";
    if (isTablet.value) return isExpanded.value ? "200px" : "60px";
    if (isLaptop.value) return isExpanded.value ? "220px" : "70px";
    return isExpanded.value ? "250px" : "80px";
  });

  const shouldShowMobileMenu = computed(() => isMobile.value);

  // Update screen size tracking
  const updateScreenSize = () => {
    if (typeof window === "undefined") return;
    windowWidth.value = window.innerWidth;

    if (windowWidth.value >= 1200) {
      screenSize.value = "desktop";
      if (!localStorage.getItem("sidebar-manually-collapsed")) {
        isExpanded.value = true;
      }
    } else if (windowWidth.value >= 1024) {
      screenSize.value = "laptop";
      if (!localStorage.getItem("sidebar-manually-collapsed")) {
        isExpanded.value = true;
      }
    } else if (windowWidth.value >= 768) {
      screenSize.value = "tablet";
      isExpanded.value = false;
      isMobileMenuOpen.value = false;
    } else {
      screenSize.value = "mobile";
      isExpanded.value = false;
      isMobileMenuOpen.value = false;
    }
  };

  // Handle scroll for mobile header
  const handleScroll = () => {
    isScrolled.value = window.scrollY > 20;
  };

  // Methods
  const setTab = (item: NavigationItem) => {
    if (item.path) {
      router.push(item.path);
    } else {
      router.push({ name: item.id });
    }
    showMenu.value = false;
  };

  const handleMobileNavClick = (item: NavigationItem) => {
    setTab(item);
    isMobileMenuOpen.value = false;
  };

  const closeMobileMenu = () => {
    isMobileMenuOpen.value = false;
  };

  const toggleMenu = () => {
    showMenu.value = !showMenu.value;
  };

  const handleMouseEnter = (itemId: string) => {
    hoveredItem.value = itemId;
  };

  const handleMouseLeave = () => {
    hoveredItem.value = null;
  };

  const toggleExpanded = () => {
    if (isMobile.value) {
      isMobileMenuOpen.value = !isMobileMenuOpen.value;
    } else {
      isExpanded.value = !isExpanded.value;
      if (isDesktop.value || isLaptop.value) {
        if (isExpanded.value) {
          localStorage.removeItem("sidebar-manually-collapsed");
        } else {
          localStorage.setItem("sidebar-manually-collapsed", "true");
        }
      }
    }
  };

  const setupSidebar = () => {
    updateScreenSize();
    window.addEventListener("resize", updateScreenSize);
    window.addEventListener("scroll", handleScroll);

    return () => {
      window.removeEventListener("resize", updateScreenSize);
      window.removeEventListener("scroll", handleScroll);
    };
  };

  return {
    hoveredItem,
    showMenu,
    isExpanded,
    isMobileMenuOpen,
    isScrolled,
    screenSize,
    isDesktop,
    isLaptop,
    isTablet,
    isMobile,
    sidebarWidth,
    shouldShowMobileMenu,
    setTab,
    toggleMenu,
    handleMouseEnter,
    handleMouseLeave,
    toggleExpanded,
    setupSidebar,
    handleMobileNavClick,
    closeMobileMenu,
  };
}
