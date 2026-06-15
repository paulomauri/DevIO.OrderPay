"use client";

import { useAppSelector } from "@/store";
import { selectActiveModal } from "@/store/uiSlice";
import CustomerFormModal from "@/features/customers/CustomerFormModal";
import ProductFormModal from "@/features/products/ProductFormModal";
import CreateOrderModal from "@/features/orders/CreateOrderModal";
import UpdateStatusModal from "@/features/orders/UpdateStatusModal";
import ConfirmDeleteModal from "@/components/ui/ConfirmDeleteModal";

export default function ModalManager() {
  const activeModal = useAppSelector(selectActiveModal);

  if (!activeModal) return null;

  switch (activeModal) {
    case "createCustomer":
    case "editCustomer":
      return <CustomerFormModal />;

    case "createProduct":
    case "editProduct":
      return <ProductFormModal />;

    case "createOrder":
      return <CreateOrderModal />;

    case "updateStatus":
      return <UpdateStatusModal />;

    case "confirmDelete":
      return <ConfirmDeleteModal />;

    default:
      return null;
  }
}
