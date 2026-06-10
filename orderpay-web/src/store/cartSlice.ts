import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "./index";

export interface CartItem {
  productId: string;
  quantity:  number;
  price:     number;
  discount:  number;
}

interface CartState {
  customerId: string | null;
  items:      CartItem[];
  isOpen:     boolean;
}

const initialState: CartState = {
  customerId: null,
  items:      [],
  isOpen:     false,
};

const cartSlice = createSlice({
  name: "cart",
  initialState,
  reducers: {
    setCustomer(state, action: PayloadAction<string>) {
      state.customerId = action.payload;
    },
    addItem(state, action: PayloadAction<CartItem>) {
      const exists = state.items.find((i) => i.productId === action.payload.productId);
      if (!exists) {
        state.items.push(action.payload);
      }
    },
    updateItem(state, action: PayloadAction<CartItem>) {
      const index = state.items.findIndex((i) => i.productId === action.payload.productId);
      if (index !== -1) {
        state.items[index] = action.payload;
      }
    },
    removeItem(state, action: PayloadAction<string>) {
      state.items = state.items.filter((i) => i.productId !== action.payload);
    },
    clearCart(state) {
      state.customerId = null;
      state.items      = [];
      state.isOpen     = false;
    },
    openCart(state) {
      state.isOpen = true;
    },
    closeCart(state) {
      state.isOpen = false;
    },
  },
});

export const { setCustomer, addItem, updateItem, removeItem, clearCart, openCart, closeCart } =
  cartSlice.actions;

export const selectCart       = (state: RootState) => state.cart;
export const selectCartItems  = (state: RootState) => state.cart.items;
export const selectCustomerId = (state: RootState) => state.cart.customerId;
export const selectCartOpen   = (state: RootState) => state.cart.isOpen;
export const selectCartTotal  = (state: RootState) =>
  state.cart.items.reduce((sum, i) => sum + i.price * i.quantity - i.discount * i.quantity, 0);

export default cartSlice.reducer;
