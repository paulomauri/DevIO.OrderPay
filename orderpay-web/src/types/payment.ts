export type PaymentType = "CREDIT" | "DEBIT" | "ACH";

export interface PaymentRequest {
  orderId:        string;
  amount:         number;
  currency:       string;
  type:           PaymentType;
  cardBrand:      string;
  last4:          string;
  expiry:         string;
  attemptNumber?: number;
}

export interface PaymentResponse {
  id:               string;
  orderId:          string;
  amount:           number;
  currency:         string;
  status:           string;
  gatewayReference: string | null;
  attemptNumber:    number;
  attemptOutcome:   string;
}
