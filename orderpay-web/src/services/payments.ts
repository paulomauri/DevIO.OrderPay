import api from "./api";
import type { PaymentRequest, PaymentResponse } from "@/types/payment";

const BASE = "/api/v1/payment";

export const paymentsService = {
  getByOrder: (orderId: string) =>
    api.get<PaymentResponse>(`${BASE}/${orderId}`).then((r) => r.data),

  pay: (data: PaymentRequest) =>
    api.post<PaymentResponse>(BASE, data).then((r) => r.data),
};
