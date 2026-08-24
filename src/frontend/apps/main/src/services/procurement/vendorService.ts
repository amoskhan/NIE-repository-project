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

export interface VendorDto {
  id?: string;
  name: string;
  code: string;
  contactPerson?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  category?: string | null;
  isActive: boolean;
  notes?: string | null;
  catalogItemCount?: number;
  createdOn?: string | null;
  updatedOn?: string | null;
  createdBy?: string | null;
  updatedBy?: string | null;
}

const vendorService = {
  async search(
    query: NieDataTableQuery,
  ): Promise<ServerDataTablePage<VendorDto>> {
    return (
      await api.post<ServerDataTablePage<VendorDto>>(
        "/api/Vendor/Search",
        toApiDataTableRequest(query),
      )
    ).data;
  },

  async getFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage> {
    return (
      await api.post<NieDataTableFilterOptionPage>(
        "/api/Vendor/GetFilterOptions",
        toApiFilterOptionsRequest(request),
      )
    ).data;
  },

  async getAll(): Promise<VendorDto[]> {
    const response = await api.get<VendorDto[]>("/api/Vendor/GetAll");
    return response.data;
  },

  async getLookup(): Promise<VendorDto[]> {
    return this.getAll();
  },

  async getById(id: string): Promise<VendorDto> {
    const response = await api.get<VendorDto>(`/api/Vendor/Get/${id}`);
    return response.data;
  },

  async save(dto: VendorDto): Promise<VendorDto> {
    const endpoint = dto.id ? "/api/Vendor/Edit" : "/api/Vendor/Save";
    const response = await api.post<VendorDto>(endpoint, dto);
    return response.data;
  },

  async delete(id: string): Promise<void> {
    await api.post(`/api/Vendor/Delete/${id}`);
  },
};

export default vendorService;
