import { ref, onMounted, onUnmounted, type Ref } from "vue";

export type SwipeDirection = "left" | "right" | "up" | "down";

interface UseSwipeOptions {
  /** Minimum distance (px) to register as a swipe. Default: 50 */
  threshold?: number;
  /** Called when a swipe is detected */
  onSwipe?: (direction: SwipeDirection) => void;
  /** Specific direction callbacks */
  onSwipeLeft?: () => void;
  onSwipeRight?: () => void;
  onSwipeUp?: () => void;
  onSwipeDown?: () => void;
}

/**
 * Composable for detecting touch swipe gestures on a target element.
 * Mobile-first — enables swipe-to-navigate, swipe-to-dismiss, etc.
 *
 * @example
 * ```vue
 * <script setup lang="ts">
 * import { useSwipe } from '@/composables/shell/useSwipe';
 *
 * const targetRef = ref<HTMLElement>();
 * const { direction, isSwiping } = useSwipe(targetRef, {
 *   onSwipeLeft: () => router.push('/next'),
 *   onSwipeRight: () => router.push('/prev'),
 * });
 * </script>
 * <template>
 *   <div ref="targetRef">Swipeable content</div>
 * </template>
 * ```
 */
export function useSwipe(
  target: Ref<HTMLElement | undefined>,
  options: UseSwipeOptions = {},
) {
  const { threshold = 50 } = options;

  const isSwiping = ref(false);
  const direction = ref<SwipeDirection | null>(null);

  let startX = 0;
  let startY = 0;
  let startTime = 0;

  function onTouchStart(e: TouchEvent) {
    const touch = e.touches[0];
    startX = touch.clientX;
    startY = touch.clientY;
    startTime = Date.now();
    isSwiping.value = false;
    direction.value = null;
  }

  function onTouchEnd(e: TouchEvent) {
    const touch = e.changedTouches[0];
    const deltaX = touch.clientX - startX;
    const deltaY = touch.clientY - startY;
    const elapsed = Date.now() - startTime;

    // Ignore very slow gestures (>1s)
    if (elapsed > 1000) return;

    const absX = Math.abs(deltaX);
    const absY = Math.abs(deltaY);

    if (absX < threshold && absY < threshold) return;

    isSwiping.value = true;

    if (absX > absY) {
      direction.value = deltaX > 0 ? "right" : "left";
    } else {
      direction.value = deltaY > 0 ? "down" : "up";
    }

    options.onSwipe?.(direction.value);

    switch (direction.value) {
      case "left":
        options.onSwipeLeft?.();
        break;
      case "right":
        options.onSwipeRight?.();
        break;
      case "up":
        options.onSwipeUp?.();
        break;
      case "down":
        options.onSwipeDown?.();
        break;
    }
  }

  onMounted(() => {
    const el = target.value;
    if (!el) return;
    el.addEventListener("touchstart", onTouchStart, { passive: true });
    el.addEventListener("touchend", onTouchEnd, { passive: true });
  });

  onUnmounted(() => {
    const el = target.value;
    if (!el) return;
    el.removeEventListener("touchstart", onTouchStart);
    el.removeEventListener("touchend", onTouchEnd);
  });

  return { isSwiping, direction };
}
