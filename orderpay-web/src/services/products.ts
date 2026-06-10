import api from "./api";
import type { ProductRequest, ProductResponse } from "@/types/product";

const BASE = "/api/v1/product";

export const productsService = {
  getAll: () =>
    api.get<ProductResponse[]>(BASE).then((r) => r.data),

  getById: (id: string) =>
    api.get<ProductResponse>(`${BASE}/${id}`).then((r) => r.data),

  create: (data: ProductRequest) =>
    api.post<ProductResponse>(BASE, data).then((r) => r.data),

  update: (id: string, data: ProductRequest) =>
    api.put<ProductResponse>(`${BASE}/${id}`, data).then((r) => r.data),

  updateSku: (id: string, sku: string) =>
    api.patch<ProductResponse>(`${BASE}/${id}/sku`, sku, {
      headers: { "Content-Type": "application/json" },
    }).then((r) => r.data),

  remove: (id: string) =>
    api.delete(`${BASE}/${id}`),
};
