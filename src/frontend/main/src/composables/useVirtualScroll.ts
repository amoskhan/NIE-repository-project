import { ref, computed, onMounted, onUnmounted, type Ref } from "vue";

interface UseVirtualScrollOptions<T> {
  /** All items to render virtually */
  items: Ref<T[]>;
  /** Fixed height of each row in pixels */
  itemHeight: number;
  /** Number of extra rows to render above/below the viewport. Default: 5 */
  overscan?: number;
}

/**
 * Composable for efficiently rendering large lists by only
 * rendering items visible in the viewport (+ overscan buffer).
 *
 * @example
 * ```vue
 * <script setup lang="ts">
 * import { useVirtualScroll } from '@/composables/useVirtualScroll';
 *
 * const containerRef = ref<HTMLElement>();
 * const allItems = ref(Array.from({ length: 10000 }, (_, i) => ({ id: i, name: `Item ${i}` })));
 *
 * const { visibleItems, totalHeight, offsetY } = useVirtualScroll(containerRef, {
 *   items: allItems,
 *   itemHeight: 48,
 * });
 * </script>
 * <template>
 *   <div ref="containerRef" class="overflow-auto h-96">
 *     <div :style="{ height: `${totalHeight}px`, position: 'relative' }">
 *       <div :style="{ transform: `translateY(${offsetY}px)` }">
 *         <div v-for="item in visibleItems" :key="item.id" class="h-12">
 *           {{ item.name }}
 *         </div>
 *       </div>
 *     </div>
 *   </div>
 * </template>
 * ```
 */
export function useVirtualScroll<T>(
  container: Ref<HTMLElement | undefined>,
  options: UseVirtualScrollOptions<T>,
) {
  const { itemHeight, overscan = 5 } = options;

  const scrollTop = ref(0);
  const containerHeight = ref(0);

  const totalHeight = computed(() => options.items.value.length * itemHeight);

  const startIndex = computed(() =>
    Math.max(0, Math.floor(scrollTop.value / itemHeight) - overscan),
  );

  const endIndex = computed(() =>
    Math.min(
      options.items.value.length,
      Math.ceil((scrollTop.value + containerHeight.value) / itemHeight) +
        overscan,
    ),
  );

  const visibleItems = computed(() =>
    options.items.value.slice(startIndex.value, endIndex.value),
  );

  const offsetY = computed(() => startIndex.value * itemHeight);

  function onScroll() {
    const el = container.value;
    if (!el) return;
    scrollTop.value = el.scrollTop;
  }

  let resizeObserver: ResizeObserver | null = null;

  onMounted(() => {
    const el = container.value;
    if (!el) return;

    containerHeight.value = el.clientHeight;
    el.addEventListener("scroll", onScroll, { passive: true });

    resizeObserver = new ResizeObserver(([entry]) => {
      containerHeight.value = entry.contentRect.height;
    });
    resizeObserver.observe(el);
  });

  onUnmounted(() => {
    const el = container.value;
    if (el) el.removeEventListener("scroll", onScroll);
    resizeObserver?.disconnect();
  });

  return { visibleItems, totalHeight, offsetY, startIndex, endIndex };
}
