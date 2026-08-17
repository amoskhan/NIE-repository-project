import api from "./api";

export interface CatalogItemDto {
  id?: number;
  name: string;
  sku?: string | null;
  description?: string | null;
  category?: string | null;
  unitOfMeasure?: string | null;
  unitPrice: number;
  isActive: boolean;
  vendorId: number;
  vendorName?: string | null;
  createdOn?: string | null;
  updatedOn?: string | null;
}

const catalogItemService = {
  async getAll(): Promise<CatalogItemDto[]> {
    const response = await api.get<CatalogItemDto[]>("/api/CatalogItem/GetAll");
    return response.data;
  },

  async getByVendor(vendorId: number): Promise<CatalogItemDto[]> {
    const response = await api.get<CatalogItemDto[]>(
      `/api/CatalogItem/GetByVendor/${vendorId}`,
    );
    return response.data;
  },

  async getById(id: number): Promise<CatalogItemDto> {
    const response = await api.get<CatalogItemDto>(
      `/api/CatalogItem/Get/${id}`,
    );
    return response.data;
  },

  async save(dto: CatalogItemDto): Promise<CatalogItemDto> {
    const endpoint = dto.id ? "/api/CatalogItem/Edit" : "/api/CatalogItem/Save";
    const response = await api.post<CatalogItemDto>(endpoint, dto);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await api.post(`/api/CatalogItem/Delete/${id}`);
  },
};

export default catalogItemService;
