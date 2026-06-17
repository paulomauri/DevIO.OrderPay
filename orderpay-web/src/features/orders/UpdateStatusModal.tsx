"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useAppDispatch, useAppSelector } from "@/store";
import { closeModal, selectActiveModal, selectModalPayload } from "@/store/uiSlice";
import Modal, { ModalBody, ModalFooter } from "@/components/ui/Modal";
import Select from "@/components/ui/Select";
import Button from "@/components/ui/Button";
import { ordersService } from "@/services/orders";
import type { OrderStatus } from "@/types/order";

const TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  Pending:              ["AwaitingPayment", "Cancelled"],
  AwaitingPayment:      ["PaymentConfirmed", "Cancelled"],
  PaymentConfirmed:     ["Processing", "CancellationRequested"],
  Processing:           ["Shipped", "CancellationRequested"],
  Shipped:              ["Delivered", "CancellationRequested"],
  Delivered:            [],
  CancellationRequested: ["Refunding"],
  Refunding:            ["Cancelled"],
  Cancelled:            [],
};

const schema = z.object({
  status: z.string().min(1, "Select a status"),
});

type Schema = z.infer<typeof schema>;

export default function UpdateStatusModal() {
  const dispatch     = useAppDispatch();
  const activeModal  = useAppSelector(selectActiveModal);
  const modalPayload = useAppSelector(selectModalPayload);
  const queryClient  = useQueryClient();

  const isOpen  = activeModal === "updateStatus";
  const orderId = isOpen ? modalPayload : null;
  const onClose = () => dispatch(closeModal());

  const { data: order } = useQuery({
    queryKey: ["order", orderId],
    queryFn:  () => ordersService.getById(orderId!),
    enabled:  !!orderId,
  });

  const currentStatus = order?.status as OrderStatus | undefined;
  const nextStatuses  = currentStatus ? TRANSITIONS[currentStatus] : [];

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Schema>({
    resolver:      zodResolver(schema),
    defaultValues: { status: "" },
  });

  useEffect(() => {
    if (isOpen) reset({ status: "" });
  }, [isOpen, reset]);

  const mutation = useMutation({
    mutationFn: ({ status }: Schema) =>
      ordersService.updateStatus(orderId!, status as OrderStatus),
    onSuccess: () => {
      toast.success("Order status updated.");
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["order", orderId] });
      onClose();
    },
    onError: () => toast.error("Failed to update status."),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Update Order Status">
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <ModalBody>
          {currentStatus && (
            <p style={{ margin: "0 0 12px", fontSize: "0.875rem" }}>
              Current status: <strong>{currentStatus}</strong>
            </p>
          )}
          <Select
            label="New status"
            error={errors.status?.message}
            disabled={nextStatuses.length === 0}
            {...register("status")}
          >
            <option value="">— select next status —</option>
            {nextStatuses.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </Select>
          {nextStatuses.length === 0 && currentStatus && (
            <p style={{ margin: "8px 0 0", fontSize: "0.8rem", color: "#6b7280" }}>
              This order has reached a terminal status.
            </p>
          )}
        </ModalBody>
        <ModalFooter>
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="submit"
            loading={mutation.isPending}
            disabled={nextStatuses.length === 0}
          >
            Update
          </Button>
        </ModalFooter>
      </form>
    </Modal>
  );
}
