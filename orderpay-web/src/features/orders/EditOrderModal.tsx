"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import styled from "styled-components";
import { useAppDispatch, useAppSelector } from "@/store";
import { closeModal, selectActiveModal, selectModalPayload } from "@/store/uiSlice";
import Modal, { ModalBody, ModalFooter } from "@/components/ui/Modal";
import Input from "@/components/ui/Input";
import Select from "@/components/ui/Select";
import Button from "@/components/ui/Button";
import { OrderStatusBadge } from "@/components/ui/Badge";
import { ordersService } from "@/services/orders";
import { productsService } from "@/services/products";
import { customersService } from "@/services/customers";
import { apiErrorMessage } from "@/services/api";
import { isOrderEditable } from "@/types/order";

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString("en-US", { year: "numeric", month: "short", day: "numeric" });
}

// One item to add — mirrors the backend item rules (incl. discount ≤ price).
const itemSchema = z
  .object({
    productId: z.string().min(1, "Select a product"),
    quantity:  z.coerce.number().int().min(1, "Min 1"),
    price:     z.coerce.number().min(0.01, "Enter price"),
    discount:  z.coerce.number().min(0).default(0),
  })
  .refine((i) => i.discount <= i.price, { message: "Discount can't exceed price", path: ["discount"] });

type ItemInput = z.input<typeof itemSchema>;
type ItemOutput = z.output<typeof itemSchema>;

const ItemList = styled.div`
  display:        flex;
  flex-direction: column;
  gap:            ${({ theme }) => theme.spacing.xs};
`;

const ItemRow = styled.div`
  display:         flex;
  align-items:     center;
  justify-content: space-between;
  padding:         ${({ theme }) => `${theme.spacing.xs} ${theme.spacing.sm}`};
  border:          1px solid ${({ theme }) => theme.colors.border};
  border-radius:   ${({ theme }) => theme.borderRadius.md};
  font-size:       ${({ theme }) => theme.typography.fontSize.sm};
`;

const RemoveBtn = styled.button`
  border:        none;
  background:    none;
  cursor:        pointer;
  color:         ${({ theme }) => theme.colors.textMuted};
  font-size:     16px;
  &:hover { color: ${({ theme }) => theme.colors.error}; }
  &:disabled { opacity: 0.4; cursor: not-allowed; }
`;

const AddRow = styled.div`
  display:               grid;
  grid-template-columns: 1fr 70px 90px 80px auto;
  gap:                   ${({ theme }) => theme.spacing.xs};
  align-items:           end;
`;

const SectionLabel = styled.p`
  margin:      ${({ theme }) => `${theme.spacing.sm} 0 0`};
  font-size:   ${({ theme }) => theme.typography.fontSize.sm};
  font-weight: ${({ theme }) => theme.typography.fontWeight.medium};
`;

const Summary = styled.dl`
  display:               grid;
  grid-template-columns: max-content 1fr;
  gap:                   6px 16px;
  margin:                0;
  align-items:           center;
  font-size:             ${({ theme }) => theme.typography.fontSize.sm};

  dt { color: ${({ theme }) => theme.colors.textMuted}; }
  dd { margin: 0; color: ${({ theme }) => theme.colors.text}; word-break: break-all; }
`;

export default function EditOrderModal() {
  const dispatch     = useAppDispatch();
  const activeModal  = useAppSelector(selectActiveModal);
  const modalPayload = useAppSelector(selectModalPayload);
  const queryClient  = useQueryClient();

  const isOpen  = activeModal === "editOrder";
  const orderId = isOpen ? modalPayload : null;
  const onClose = () => dispatch(closeModal());

  const { data: order } = useQuery({
    queryKey: ["order", orderId],
    queryFn:  () => ordersService.getById(orderId!),
    enabled:  !!orderId,
  });

  const { data: products } = useQuery({
    queryKey: ["products"],
    queryFn:  productsService.getAll,
    enabled:  isOpen,
  });

  const { data: customers } = useQuery({
    queryKey: ["customers"],
    queryFn:  customersService.getAll,
    enabled:  isOpen,
  });

  const productName  = (id: string) => products?.find((p) => p.id === id)?.name ?? id.slice(0, 8);
  const customerName = (id: string) => customers?.find((c) => c.id === id)?.name ?? id.slice(0, 8);

  // Guard: once the order is past AwaitingPayment its items are locked.
  const locked = order ? !isOrderEditable(order.status) : false;

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["orders"] });
    queryClient.invalidateQueries({ queryKey: ["order", orderId] });
  };

  const { register, handleSubmit, reset, formState: { errors } } = useForm<ItemInput, unknown, ItemOutput>({
    resolver:      zodResolver(itemSchema),
    defaultValues: { productId: "", quantity: 1, price: 0, discount: 0 },
  });

  const addMutation = useMutation({
    mutationFn: (item: ItemOutput) => ordersService.addItem(orderId!, item),
    onSuccess: () => {
      toast.success("Item added.");
      invalidate();
      reset({ productId: "", quantity: 1, price: 0, discount: 0 });
    },
    onError: (err) => toast.error(apiErrorMessage(err, "Failed to add item.")),
  });

  const removeMutation = useMutation({
    mutationFn: (itemId: string) => ordersService.removeItem(orderId!, itemId),
    onSuccess: () => {
      toast.success("Item removed.");
      invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, "Failed to remove item.")),
  });

  const items = order?.items ?? [];

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Edit Order" maxWidth="620px">
      <ModalBody>
        {order && (
          <Summary>
            <dt>Order ID</dt><dd>{order.id}</dd>
            <dt>Customer</dt><dd>{customerName(order.customerId)}</dd>
            <dt>Description</dt><dd>{order.details || "—"}</dd>
            <dt>Created</dt><dd>{formatDate(order.orderDate)}</dd>
            <dt>Status</dt><dd><OrderStatusBadge status={order.status} /></dd>
          </Summary>
        )}

        {locked && (
          <p style={{ margin: 0, fontSize: "0.85rem", color: "#b45309" }}>
            This order is <strong>{order?.status}</strong> and can no longer be edited.
          </p>
        )}

        <SectionLabel>Items</SectionLabel>
        <ItemList>
          {items.map((item) => (
            <ItemRow key={item.id}>
              <span>
                {productName(item.productId)} — qty {item.quantity} · ${item.price.toFixed(2)}
                {item.discount > 0 && ` · -$${item.discount.toFixed(2)}`}
              </span>
              <RemoveBtn
                type="button"
                aria-label="Remove item"
                disabled={locked || items.length === 1 || removeMutation.isPending}
                onClick={() => removeMutation.mutate(item.id)}
              >
                ✕
              </RemoveBtn>
            </ItemRow>
          ))}
        </ItemList>

        {!locked && (
          <>
            <SectionLabel>Add item</SectionLabel>
            <form onSubmit={handleSubmit((d) => addMutation.mutate(d))}>
              <AddRow>
                <Select aria-label="Product" error={errors.productId?.message} {...register("productId")}>
                  <option value="">— product —</option>
                  {products?.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </Select>
                <Input aria-label="Qty" type="number" min={1} placeholder="Qty"
                  error={errors.quantity?.message} {...register("quantity")} />
                <Input aria-label="Price" type="number" step="0.01" min={0} placeholder="Price"
                  error={errors.price?.message} {...register("price")} />
                <Input aria-label="Discount" type="number" step="0.01" min={0} placeholder="Disc."
                  error={errors.discount?.message} {...register("discount")} />
                <Button type="submit" size="sm" loading={addMutation.isPending}>Add</Button>
              </AddRow>
            </form>
          </>
        )}
      </ModalBody>
      <ModalFooter>
        <Button type="button" variant="ghost" onClick={onClose}>Close</Button>
      </ModalFooter>
    </Modal>
  );
}
