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
import Input from "@/components/ui/Input";
import Button from "@/components/ui/Button";
import { ordersService } from "@/services/orders";
import { paymentsService } from "@/services/payments";
import { apiErrorMessage } from "@/services/api";

// Card fields mirror the backend PaymentRequestValidator. Amount/currency/type are
// derived from the order, not entered by the user.
const schema = z.object({
  cardBrand: z.string().min(1, "Card brand is required"),
  last4:     z.string().regex(/^\d{4}$/, "Enter the last 4 digits"),
  expiry:    z.string().regex(/^(0[1-9]|1[0-2])\/\d{2}$/, "Use MM/YY"),
});

type Schema = z.infer<typeof schema>;

export default function PayOrderModal() {
  const dispatch     = useAppDispatch();
  const activeModal  = useAppSelector(selectActiveModal);
  const modalPayload = useAppSelector(selectModalPayload);
  const queryClient  = useQueryClient();

  const isOpen  = activeModal === "payOrder";
  const orderId = isOpen ? modalPayload : null;
  const onClose = () => dispatch(closeModal());

  const { data: order } = useQuery({
    queryKey: ["order", orderId],
    queryFn:  () => ordersService.getById(orderId!),
    enabled:  !!orderId,
  });

  const amount = order ? order.totalPrice - order.totalDiscount : 0;

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Schema>({
    resolver: zodResolver(schema),
    defaultValues: { cardBrand: "", last4: "", expiry: "" },
  });

  useEffect(() => {
    if (isOpen) reset({ cardBrand: "", last4: "", expiry: "" });
  }, [isOpen, reset]);

  const mutation = useMutation({
    mutationFn: (data: Schema) =>
      paymentsService.pay({
        orderId:   orderId!,
        amount,
        currency:  "USD",
        type:      "CREDIT",
        cardBrand: data.cardBrand,
        last4:     data.last4,
        expiry:    data.expiry,
      }),
    onSuccess: (payment) => {
      if (payment.status === "Captured") {
        toast.success("Payment captured.");
        queryClient.invalidateQueries({ queryKey: ["orders"] });
        queryClient.invalidateQueries({ queryKey: ["order", orderId] });
        onClose();
      } else {
        // 200 but not captured = declined; keep the modal open for a retry.
        toast.error("Payment declined. Try another card.");
      }
    },
    onError: (err) => toast.error(apiErrorMessage(err, "Payment failed.")),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Pay Order">
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <ModalBody>
          <p style={{ margin: 0, fontSize: "0.875rem" }}>
            Amount due: <strong>${amount.toFixed(2)}</strong>
          </p>
          <Input
            label="Card brand"
            placeholder="Visa"
            error={errors.cardBrand?.message}
            {...register("cardBrand")}
          />
          <Input
            label="Card number (last 4)"
            placeholder="4242"
            inputMode="numeric"
            maxLength={4}
            error={errors.last4?.message}
            {...register("last4")}
          />
          <Input
            label="Expiry"
            placeholder="MM/YY"
            error={errors.expiry?.message}
            {...register("expiry")}
          />
        </ModalBody>
        <ModalFooter>
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={mutation.isPending} disabled={!order}>
            Pay ${amount.toFixed(2)}
          </Button>
        </ModalFooter>
      </form>
    </Modal>
  );
}
