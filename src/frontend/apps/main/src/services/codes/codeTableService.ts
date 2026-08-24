import api from "../core/api";

export const CodeTableType = {
  Title: "TITLE",
  UserType: "USER_TYPE",
  VendorCategory: "VENDOR_CATEGORY",
  CatalogCategory: "CATALOG_CATEGORY",
  UnitOfMeasure: "UNIT_OF_MEASURE",
  DeliveryLocation: "DELIVERY_LOCATION",
  Currency: "CURRENCY",
} as const;

export type CodeTableTypeValue =
  (typeof CodeTableType)[keyof typeof CodeTableType];

interface CodeTableDto {
  id: string;
  displayName: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface CodeTableOption {
  id: string;
  label: string;
  value: string;
  description?: string | null;
  displayOrder: number;
  isActive: boolean;
}

const codeTableCache = new Map<CodeTableTypeValue, CodeTableOption[]>();
const pendingRequests = new Map<
  CodeTableTypeValue,
  Promise<CodeTableOption[]>
>();

function mapCodeTableOption(code: CodeTableDto): CodeTableOption {
  return {
    id: code.id,
    label: code.displayName,
    value: code.displayName,
    description: code.description,
    displayOrder: code.displayOrder,
    isActive: code.isActive,
  };
}

async function fetchByType(
  type: CodeTableTypeValue,
): Promise<CodeTableOption[]> {
  const response = await api.get<CodeTableDto[]>("/api/Code/GetAllByCodeType", {
    params: { codeType: type },
  });

  return response.data
    .filter((code) => code.isActive)
    .sort((left, right) => left.displayOrder - right.displayOrder)
    .map(mapCodeTableOption);
}

const codeTableService = {
  async getByType(
    type: CodeTableTypeValue,
    forceRefresh = false,
  ): Promise<CodeTableOption[]> {
    if (!forceRefresh && codeTableCache.has(type)) {
      return codeTableCache.get(type) ?? [];
    }

    if (!forceRefresh && pendingRequests.has(type)) {
      return pendingRequests.get(type) ?? Promise.resolve([]);
    }

    const request = fetchByType(type)
      .then((options) => {
        codeTableCache.set(type, options);
        pendingRequests.delete(type);
        return options;
      })
      .catch((error) => {
        pendingRequests.delete(type);
        throw error;
      });

    pendingRequests.set(type, request);
    return request;
  },
};

export default codeTableService;
