import api from "./api";

export interface VendorDto {
  id?: number;
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
  async getAll(): Promise<VendorDto[]> {
    const response = await api.get<VendorDto[]>("/api/Vendor/GetAll");
    return response.data;
  },

  async getById(id: number): Promise<VendorDto> {
    const response = await api.get<VendorDto>(`/api/Vendor/Get/${id}`);
    return response.data;
  },

  async save(dto: VendorDto): Promise<VendorDto> {
    const endpoint = dto.id ? "/api/Vendor/Edit" : "/api/Vendor/Save";
    const response = await api.post<VendorDto>(endpoint, dto);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await api.post(`/api/Vendor/Delete/${id}`);
  },
};

export default vendorService;
