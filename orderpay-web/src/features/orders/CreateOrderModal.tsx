"use client";

import { useEffect } from "react";
import { useForm, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import styled from "styled-components";
import { useAppDispatch, useAppSelector } from "@/store";
import { closeModal, selectActiveModal } from "@/store/uiSlice";
import Modal, { ModalBody, ModalFooter } from "@/components/ui/Modal";
import Input from "@/components/ui/Input";
import Select from "@/components/ui/Select";
import Button from "@/components/ui/Button";
import { customersService } from "@/services/customers";
import { productsService } from "@/services/products";
import { ordersService } from "@/services/orders";

const itemSchema = z.object({
  productId: z.string().min(1, "Select a product"),
  quantity:  z.coerce.number().int().min(1, "Min 1"),
  price:     z.coerce.number().min(0.01, "Enter price"),
  discount:  z.coerce.number().min(0).default(0),
});

const schema = z.object({
  customerId: z.string().min(1, "Select a customer"),
  details:    z.string().optional(),
  items:      z.array(itemSchema).min(1, "Add at least one item"),
});

// z.coerce.number() gives an `unknown` input type but a `number` output type,
// so the form's field values (input) and submit payload (output) differ.
type FormInput  = z.input<typeof schema>;
type FormOutput = z.output<typeof schema>;

const defaultItem = { productId: "", quantity: 1, price: 0, discount: 0 };

const ItemsSection = styled.div`
  display:        flex;
  flex-direction: column;
  gap:            ${({ theme }) => theme.spacing.sm};
`;

const SectionLabel = styled.p`
  font-size:   ${({ theme }) => theme.typography.fontSize.sm};
  font-weight: ${({ theme }) => theme.typography.fontWeight.medium};
  color:       ${({ theme }) => theme.colors.text};
  margin:      0;
`;

const ItemRow = styled.div`
  display:     grid;
  grid-template-columns: 1fr 70px 90px 80px 36px;
  gap:         ${({ theme }) => theme.spacing.xs};
  align-items: end;
`;

const RemoveBtn = styled.button`
  height:        38px;
  width:         36px;
  border:        1px solid ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.borderRadius.md};
  background:    ${({ theme }) => theme.colors.surface};
  color:         ${({ theme }) => theme.colors.textMuted};
  cursor:        pointer;
  font-size:     16px;
  display:       flex;
  align-items:   center;
  justify-content: center;
  transition:    color 0.15s, border-color 0.15s;

  &:hover {
    color:        ${({ theme }) => theme.colors.error};
    border-color: ${({ theme }) => theme.colors.error};
  }
`;

const ErrorMsg = styled.span`
  font-size: ${({ theme }) => theme.typography.fontSize.xs};
  color:     ${({ theme }) => theme.colors.error};
`;

export default function CreateOrderModal() {
  const dispatch    = useAppDispatch();
  const activeModal = useAppSelector(selectActiveModal);
  const queryClient = useQueryClient();
  const isOpen      = activeModal === "createOrder";
  const onClose     = () => dispatch(closeModal());

  const { data: customers } = useQuery({
    queryKey: ["customers"],
    queryFn:  customersService.getAll,
    enabled:  isOpen,
  });

  const { data: products } = useQuery({
    queryKey: ["products"],
    queryFn:  productsService.getAll,
    enabled:  isOpen,
  });

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<FormInput, unknown, FormOutput>({
    resolver:      zodResolver(schema),
    defaultValues: { customerId: "", details: "", items: [defaultItem] },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  useEffect(() => {
    if (!isOpen) reset({ customerId: "", details: "", items: [defaultItem] });
  }, [isOpen, reset]);

  const mutation = useMutation({
    mutationFn: (data: FormOutput) =>
      ordersService.create({
        customerId: data.customerId,
        details:    data.details ?? "",
        items:      data.items,
      }),
    onSuccess: () => {
      toast.success("Order created.");
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      onClose();
    },
    onError: () => toast.error("Failed to create order."),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="New Order" maxWidth="680px">
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <ModalBody>
          <Select
            label="Customer"
            error={errors.customerId?.message}
            {...register("customerId")}
          >
            <option value="">— select customer —</option>
            {customers?.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </Select>

          <Input
            label="Details (optional)"
            placeholder="Order notes..."
            {...register("details")}
          />

          <ItemsSection>
            <SectionLabel>Items</SectionLabel>
            {fields.map((field, idx) => (
              <ItemRow key={field.id}>
                <Select
                  aria-label="Product"
                  error={errors.items?.[idx]?.productId?.message}
                  {...register(`items.${idx}.productId`)}
                >
                  <option value="">— product —</option>
                  {products?.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </Select>
                <Input
                  aria-label="Qty"
                  type="number"
                  min={1}
                  placeholder="Qty"
                  error={errors.items?.[idx]?.quantity?.message}
                  {...register(`items.${idx}.quantity`)}
                />
                <Input
                  aria-label="Price"
                  type="number"
                  step="0.01"
                  min={0}
                  placeholder="Price"
                  error={errors.items?.[idx]?.price?.message}
                  {...register(`items.${idx}.price`)}
                />
                <Input
                  aria-label="Discount"
                  type="number"
                  step="0.01"
                  min={0}
                  placeholder="Disc."
                  {...register(`items.${idx}.discount`)}
                />
                <RemoveBtn
                  type="button"
                  onClick={() => remove(idx)}
                  disabled={fields.length === 1}
                  aria-label="Remove item"
                >
                  ✕
                </RemoveBtn>
              </ItemRow>
            ))}
            {errors.items?.root && (
              <ErrorMsg>{errors.items.root.message}</ErrorMsg>
            )}
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => append(defaultItem)}
            >
              + Add item
            </Button>
          </ItemsSection>

        </ModalBody>
        <ModalFooter>
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={mutation.isPending}>
            Create Order
          </Button>
        </ModalFooter>
      </form>
    </Modal>
  );
}
