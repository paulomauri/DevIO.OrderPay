export interface CustomerResponse {
  id:     string;
  name:   string;
  email:  string;
  mobile: string;
}

export interface CustomerRequest {
  name:   string;
  email:  string;
  cpf:    string;
  mobile: string;
}
