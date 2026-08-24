import { ref } from "vue";
import codeTableService, {
  type CodeTableTypeValue,
} from "@/services/codes/codeTableService";
import type { ListFilterOption } from "@/utils/listFilterOptions";

export function useCodeTableOptions(codeTypes: readonly CodeTableTypeValue[]) {
  const optionsByType = ref<
    Partial<Record<CodeTableTypeValue, ListFilterOption[]>>
  >({});
  const loading = ref(false);

  async function loadCodeTableOptions(forceRefresh = false): Promise<void> {
    const uniqueTypes = [...new Set(codeTypes)];

    if (!uniqueTypes.length) {
      return;
    }

    loading.value = true;

    try {
      const results = await Promise.allSettled(
        uniqueTypes.map((type) =>
          codeTableService.getByType(type, forceRefresh),
        ),
      );

      const nextOptions = { ...optionsByType.value };

      results.forEach((result, index) => {
        const codeType = uniqueTypes[index];

        if (result.status === "fulfilled") {
          nextOptions[codeType] = result.value.map((option) => ({
            label: option.label,
            value: option.value,
            count: 0,
          }));
          return;
        }

        nextOptions[codeType] = nextOptions[codeType] ?? [];
      });

      optionsByType.value = nextOptions;
    } finally {
      loading.value = false;
    }
  }

  return {
    codeTableOptions: optionsByType,
    loadingCodeTables: loading,
    loadCodeTableOptions,
  };
}
