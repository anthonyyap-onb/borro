import apiClient from '../../lib/apiClient';

export interface ItemAttributesDto {
  mileage?: number;
  transmission?: string;
  bedrooms?: number;
  megapixels?: number;
  brand?: string;
  condition?: string;
}

export interface ItemDto {
  id: string;
  ownerId: string;
  ownerName: string;
  title: string;
  description: string;
  dailyPrice: number;
  location: string;
  category: string;
  attributes: ItemAttributesDto;
  instantBookEnabled: boolean;
  handoverOptions: string[];
  imageUrls: string[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateItemPayload {
  title: string;
  description: string;
  dailyPrice: number;
  location: string;
  category: string;
  instantBookEnabled: boolean;
  handoverOptions: string[];
  mileage?: number;
  transmission?: string;
  bedrooms?: number;
  megapixels?: number;
  brand?: string;
  condition?: string;
}

export interface SearchParams {
  category?: string;
  location?: string;
  maxPrice?: number;
}

export const itemApi = {
  search: (params: SearchParams) =>
    apiClient.get<ItemDto[]>('/api/items/search', { params }),

  getById: (id: string) =>
    apiClient.get<ItemDto>(`/api/items/${id}`),

  create: (payload: CreateItemPayload) =>
    apiClient.post<ItemDto>('/api/items', payload),

  uploadImage: (itemId: string, file: File) => {
    const form = new FormData();
    form.append('itemId', itemId);
    form.append('file', file);
    return apiClient.post<{ url: string }>('/api/items/images', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
