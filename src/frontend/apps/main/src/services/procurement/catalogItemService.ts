import api from "../core/api";
import type {
  NieDataTableFilterOptionPage,
  NieDataTableFilterOptionsRequest,
  NieDataTableQuery,
} from "@nie/ui";
import type { ServerDataTablePage } from "@/composables/data-tables/useServerDataTable";
import {
  toApiDataTableRequest,
  toApiFilterOptionsRequest,
} from "../core/dataTableApi";

export interface CatalogItemDto {
  id?: string;
  name: string;
  sku?: string | null;
  description?: string | null;
  category?: string | null;
  unitOfMeasure?: string | null;
  unitPrice: number;
  isActive: boolean;
  vendorId: string;
  vendorName?: string | null;
  createdOn?: string | null;
  updatedOn?: string | null;
}

const catalogItemService = {
  async search(
    query: NieDataTableQuery,
  ): Promise<ServerDataTablePage<CatalogItemDto>> {
    return (
      await api.post<ServerDataTablePage<CatalogItemDto>>(
        "/api/CatalogItem/Search",
        toApiDataTableRequest(query),
      )
    ).data;
  },

  async getFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage> {
    return (
      await api.post<NieDataTableFilterOptionPage>(
        "/api/CatalogItem/GetFilterOptions",
        toApiFilterOptionsRequest(request),
      )
    ).data;
  },

  async getAll(): Promise<CatalogItemDto[]> {
    const response = await api.get<CatalogItemDto[]>(
      "/api/CatalogItem/GetAll",
    );
    return response.data;
  },

  async getByVendor(vendorId: string): Promise<CatalogItemDto[]> {
    const response = await api.get<CatalogItemDto[]>(
      `/api/CatalogItem/GetByVendor/${vendorId}`,
    );
    return response.data;
  },

  async getById(id: string): Promise<CatalogItemDto> {
    const response = await api.get<CatalogItemDto>(
      `/api/CatalogItem/Get/${id}`,
    );
    return response.data;
  },

  async save(dto: CatalogItemDto): Promise<CatalogItemDto> {
    const endpoint = dto.id
      ? "/api/CatalogItem/Edit"
      : "/api/CatalogItem/Save";
    const response = await api.post<CatalogItemDto>(endpoint, dto);
    return response.data;
  },

  async delete(id: string): Promise<void> {
    await api.post(`/api/CatalogItem/Delete/${id}`);
  },
};

export default catalogItemService;
