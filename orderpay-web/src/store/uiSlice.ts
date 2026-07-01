import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "./index";

type ModalName =
  | "createCustomer"
  | "editCustomer"
  | "createProduct"
  | "editProduct"
  | "createOrder"
  | "editOrder"
  | "updateStatus"
  | "payOrder"
  | "confirmDelete"
  | null;

interface UiState {
  sidebarOpen: boolean;
  activeModal: ModalName;
  modalPayload: string | null; // entity id being edited/deleted
  // Orders just paid, awaiting the async PaymentConfirmed advance (orderId → started-at ms).
  // Drives the optimistic "Payment Processing…" badge + table polling.
  settlingOrders: Record<string, number>;
}

const initialState: UiState = {
  sidebarOpen: true,
  activeModal:  null,
  modalPayload: null,
  settlingOrders: {},
};

const uiSlice = createSlice({
  name: "ui",
  initialState,
  reducers: {
    toggleSidebar(state) {
      state.sidebarOpen = !state.sidebarOpen;
    },
    openModal(state, action: PayloadAction<{ modal: ModalName; payload?: string }>) {
      state.activeModal  = action.payload.modal;
      state.modalPayload = action.payload.payload ?? null;
    },
    closeModal(state) {
      state.activeModal  = null;
      state.modalPayload = null;
    },
    markOrderSettling(state, action: PayloadAction<string>) {
      state.settlingOrders[action.payload] = Date.now();
    },
    clearOrderSettling(state, action: PayloadAction<string>) {
      delete state.settlingOrders[action.payload];
    },
  },
});

export const { toggleSidebar, openModal, closeModal, markOrderSettling, clearOrderSettling } =
  uiSlice.actions;

export const selectSidebarOpen    = (state: RootState) => state.ui.sidebarOpen;
export const selectActiveModal    = (state: RootState) => state.ui.activeModal;
export const selectModalPayload   = (state: RootState) => state.ui.modalPayload;
export const selectSettlingOrders = (state: RootState) => state.ui.settlingOrders;

export default uiSlice.reducer;
