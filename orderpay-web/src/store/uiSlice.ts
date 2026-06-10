import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "./index";

type ModalName =
  | "createCustomer"
  | "editCustomer"
  | "createProduct"
  | "editProduct"
  | "createOrder"
  | "updateStatus"
  | "confirmDelete"
  | null;

interface UiState {
  sidebarOpen: boolean;
  activeModal: ModalName;
  modalPayload: string | null; // entity id being edited/deleted
}

const initialState: UiState = {
  sidebarOpen: true,
  activeModal:  null,
  modalPayload: null,
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
  },
});

export const { toggleSidebar, openModal, closeModal } = uiSlice.actions;

export const selectSidebarOpen  = (state: RootState) => state.ui.sidebarOpen;
export const selectActiveModal  = (state: RootState) => state.ui.activeModal;
export const selectModalPayload = (state: RootState) => state.ui.modalPayload;

export default uiSlice.reducer;
