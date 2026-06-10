export type OrderStatus =
  | "Pending"
  | "AwaitingPayment"
  | "PaymentConfirmed"
  | "Processing"
  | "Shipped"
  | "Delivered"
  | "CancellationRequested"
  | "Refunding"
  | "Cancelled";

export interface OrderItemResponse {
  id:        string;
  productId: string;
  quantity:  number;
  price:     number;
  discount:  number;
}

export interface OrderResponse {
  id:            string;
  customerId:    string;
  details:       string;
  orderDate:     string;
  totalPrice:    number;
  totalDiscount: number;
  deliveryDate:  string | null;
  status:        OrderStatus;
  items:         OrderItemResponse[];
  createdAt:     string;
  updatedAt:     string;
}

export interface OrderItemRequest {
  productId: string;
  quantity:  number;
  price:     number;
  discount:  number;
}

export interface OrderRequest {
  customerId: string;
  details:    string;
  items:      OrderItemRequest[];
}
