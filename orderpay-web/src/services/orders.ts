import api from "./api";
import type { OrderItemRequest, OrderRequest, OrderResponse, OrderStatus } from "@/types/order";

const BASE = "/api/v1/order";

export const ordersService = {
  getAll: () =>
    api.get<OrderResponse[]>(BASE).then((r) => r.data),

  getById: (id: string) =>
    api.get<OrderResponse>(`${BASE}/${id}`).then((r) => r.data),

  getByCustomer: (customerId: string) =>
    api.get<OrderResponse[]>(`${BASE}/customer/${customerId}`).then((r) => r.data),

  create: (data: OrderRequest) =>
    api.post<OrderResponse>(BASE, data).then((r) => r.data),

  remove: (id: string) =>
    api.delete(`${BASE}/${id}`),

  updateStatus: (id: string, status: OrderStatus) =>
    api.patch<OrderResponse>(`${BASE}/${id}/status`, status, {
      headers: { "Content-Type": "application/json" },
    }).then((r) => r.data),

  updateDeliveryDate: (id: string, date: string) =>
    api.patch<OrderResponse>(`${BASE}/${id}/delivery-date`, date, {
      headers: { "Content-Type": "application/json" },
    }).then((r) => r.data),

  addItem: (id: string, item: OrderItemRequest) =>
    api.post<OrderResponse>(`${BASE}/${id}/items`, item).then((r) => r.data),

  removeItem: (id: string, itemId: string) =>
    api.delete<OrderResponse>(`${BASE}/${id}/items/${itemId}`).then((r) => r.data),
};
