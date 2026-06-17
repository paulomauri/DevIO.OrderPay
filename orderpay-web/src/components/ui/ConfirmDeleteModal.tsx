"use client";

import styled from "styled-components";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useAppDispatch, useAppSelector } from "@/store";
import { closeModal, selectActiveModal, selectModalPayload } from "@/store/uiSlice";
import Modal, { ModalBody, ModalFooter } from "@/components/ui/Modal";
import Button from "@/components/ui/Button";
import { customersService } from "@/services/customers";
import { productsService } from "@/services/products";
import { ordersService } from "@/services/orders";

const Message = styled.p`
  margin: 0;
  font-size: ${({ theme }) => theme.typography.fontSize.sm};
  color:     ${({ theme }) => theme.colors.text};
`;

const LABELS: Record<string, string> = {
  customer: "customer",
  product:  "product",
  order:    "order",
};

function resolveDeleteFn(entityType: string, id: string) {
  switch (entityType) {
    case "customer": return () => customersService.remove(id);
    case "product":  return () => productsService.remove(id);
    case "order":    return () => ordersService.remove(id);
    default:         throw new Error(`Unknown entity type: ${entityType}`);
  }
}

function resolveInvalidateKey(entityType: string): string {
  switch (entityType) {
    case "customer": return "customers";
    case "product":  return "products";
    case "order":    return "orders";
    default:         return entityType;
  }
}

export default function ConfirmDeleteModal() {
  const dispatch     = useAppDispatch();
  const activeModal  = useAppSelector(selectActiveModal);
  const modalPayload = useAppSelector(selectModalPayload);
  const queryClient  = useQueryClient();

  const isOpen = activeModal === "confirmDelete";
  const onClose = () => dispatch(closeModal());

  // payload format: "entityType:uuid"
  const [entityType = "", entityId = ""] = (modalPayload ?? "").split(":");
  const label = LABELS[entityType] ?? entityType;

  const mutation = useMutation({
    mutationFn: resolveDeleteFn(entityType, entityId),
    onSuccess:  () => {
      toast.success(`${label.charAt(0).toUpperCase() + label.slice(1)} deleted.`);
      queryClient.invalidateQueries({ queryKey: [resolveInvalidateKey(entityType)] });
      onClose();
    },
    onError: () => toast.error(`Failed to delete ${label}.`),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Confirm Delete">
      <ModalBody>
        <Message>
          Are you sure you want to delete this {label}? This action cannot be undone.
        </Message>
      </ModalBody>
      <ModalFooter>
        <Button type="button" variant="ghost" onClick={onClose}>
          Cancel
        </Button>
        <Button
          type="button"
          variant="danger"
          loading={mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          Delete
        </Button>
      </ModalFooter>
    </Modal>
  );
}
