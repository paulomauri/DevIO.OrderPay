import api from "./api";
import type { CustomerRequest, CustomerResponse } from "@/types/customer";

const BASE = "/api/v1/customer";

export const customersService = {
  getAll: () =>
    api.get<CustomerResponse[]>(BASE).then((r) => r.data),

  getById: (id: string) =>
    api.get<CustomerResponse>(`${BASE}/${id}`).then((r) => r.data),

  create: (data: CustomerRequest) =>
    api.post<CustomerResponse>(BASE, data).then((r) => r.data),

  update: (id: string, data: CustomerRequest) =>
    api.put<CustomerResponse>(`${BASE}/${id}`, data).then((r) => r.data),

  remove: (id: string) =>
    api.delete(`${BASE}/${id}`),
};
