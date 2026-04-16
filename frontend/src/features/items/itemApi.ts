// frontend/src/features/items/itemApi.ts
import apiClient from '../../lib/apiClient';

export interface ItemAttributes {
  [key: string]: string | number | boolean;
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
  attributes: ItemAttributes;
  instantBookEnabled: boolean;
  handoverOptions: string[];
  imageUrls: string[];
  createdAtUtc: string;
}

export interface CreateItemPayload {
  title: string;
  description: string;
  dailyPrice: number;
  location: string;
  category: string;
  attributes: ItemAttributes;
  instantBookEnabled: boolean;
  handoverOptions: string[];
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
