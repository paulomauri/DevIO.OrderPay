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
import { customersService } from "@/services/customers";

const schema = z.object({
  name:   z.string().min(1, "Name is required"),
  email:  z.string().email("Invalid email"),
  cpf:    z.string().min(11, "CPF must be at least 11 digits"),
  mobile: z.string().min(8, "Mobile number is required"),
});

type Schema = z.infer<typeof schema>;

export default function CustomerFormModal() {
  const dispatch     = useAppDispatch();
  const activeModal  = useAppSelector(selectActiveModal);
  const modalPayload = useAppSelector(selectModalPayload);
  const queryClient  = useQueryClient();

  const isCreate = activeModal === "createCustomer";
  const isEdit   = activeModal === "editCustomer";
  const isOpen   = isCreate || isEdit;
  const editId   = isEdit ? modalPayload : null;

  const { data: existing } = useQuery({
    queryKey: ["customer", editId],
    queryFn:  () => customersService.getById(editId!),
    enabled:  !!editId,
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Schema>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", email: "", cpf: "", mobile: "" },
  });

  useEffect(() => {
    if (isOpen && existing) {
      reset({ name: existing.name, email: existing.email, cpf: "", mobile: existing.mobile });
    } else if (isOpen && isCreate) {
      reset({ name: "", email: "", cpf: "", mobile: "" });
    }
  }, [isOpen, isCreate, existing, reset]);

  const onClose = () => dispatch(closeModal());

  const mutation = useMutation({
    mutationFn: (data: Schema) =>
      editId
        ? customersService.update(editId, data)
        : customersService.create(data),
    onSuccess: () => {
      toast.success(editId ? "Customer updated." : "Customer created.");
      queryClient.invalidateQueries({ queryKey: ["customers"] });
      onClose();
    },
    onError: () => toast.error("Failed to save customer."),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isCreate ? "New Customer" : "Edit Customer"}>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <ModalBody>
          <Input
            label="Name"
            placeholder="Full name"
            error={errors.name?.message}
            {...register("name")}
          />
          <Input
            label="Email"
            type="email"
            placeholder="email@example.com"
            error={errors.email?.message}
            {...register("email")}
          />
          {isCreate && (
            <Input
              label="CPF"
              placeholder="000.000.000-00"
              error={errors.cpf?.message}
              {...register("cpf")}
            />
          )}
          <Input
            label="Mobile"
            placeholder="+55 11 90000-0000"
            error={errors.mobile?.message}
            {...register("mobile")}
          />
          {mutation.isError && (
            <span style={{ color: "red", fontSize: "0.8rem" }}>
              Failed to save customer. Please try again.
            </span>
          )}
        </ModalBody>
        <ModalFooter>
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={mutation.isPending}>
            {isCreate ? "Create" : "Save"}
          </Button>
        </ModalFooter>
      </form>
    </Modal>
  );
}
