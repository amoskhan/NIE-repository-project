import { ref, onMounted, onUnmounted, type Ref } from "vue";

interface UsePullToRefreshOptions {
  /** Minimum pull distance (px) to trigger refresh. Default: 80 */
  threshold?: number;
  /** Callback invoked when the user completes a pull-to-refresh gesture */
  onRefresh: () => void | Promise<void>;
}

/**
 * Composable for pull-to-refresh gesture on mobile.
 * Only activates when the scroll container is at the top.
 *
 * @example
 * ```vue
 * <script setup lang="ts">
 * import { usePullToRefresh } from '@/composables/usePullToRefresh';
 *
 * const containerRef = ref<HTMLElement>();
 * const { isRefreshing, pullDistance } = usePullToRefresh(containerRef, {
 *   onRefresh: () => fetchData(),
 * });
 * </script>
 * <template>
 *   <div ref="containerRef">
 *     <div v-if="pullDistance > 0" class="text-center text-sm text-gray-400 py-2">
 *       {{ isRefreshing ? 'Refreshing…' : 'Pull to refresh' }}
 *     </div>
 *     <!-- content -->
 *   </div>
 * </template>
 * ```
 */
export function usePullToRefresh(
  target: Ref<HTMLElement | undefined>,
  options: UsePullToRefreshOptions,
) {
  const { threshold = 80 } = options;

  const isRefreshing = ref(false);
  const pullDistance = ref(0);

  let startY = 0;
  let pulling = false;

  function onTouchStart(e: TouchEvent) {
    const el = target.value;
    if (!el || el.scrollTop > 0) return;
    startY = e.touches[0].clientY;
    pulling = true;
  }

  function onTouchMove(e: TouchEvent) {
    if (!pulling || isRefreshing.value) return;
    const delta = e.touches[0].clientY - startY;
    if (delta > 0) {
      pullDistance.value = Math.min(delta, threshold * 1.5);
    }
  }

  async function onTouchEnd() {
    if (!pulling) return;
    pulling = false;

    if (pullDistance.value >= threshold && !isRefreshing.value) {
      isRefreshing.value = true;
      try {
        await options.onRefresh();
      } finally {
        isRefreshing.value = false;
      }
    }
    pullDistance.value = 0;
  }

  onMounted(() => {
    const el = target.value;
    if (!el) return;
    el.addEventListener("touchstart", onTouchStart, { passive: true });
    el.addEventListener("touchmove", onTouchMove, { passive: true });
    el.addEventListener("touchend", onTouchEnd);
  });

  onUnmounted(() => {
    const el = target.value;
    if (!el) return;
    el.removeEventListener("touchstart", onTouchStart);
    el.removeEventListener("touchmove", onTouchMove);
    el.removeEventListener("touchend", onTouchEnd);
  });

  return { isRefreshing, pullDistance };
}
