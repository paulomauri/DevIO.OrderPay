export interface ProductResponse {
  id:          string;
  name:        string;
  sku:         string;
  description: string;
  createdAt:   string | null;
  updatedAt:   string | null;
}

export interface ProductRequest {
  name:        string;
  sku:         string;
  description: string;
}
