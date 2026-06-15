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
import { productsService } from "@/services/products";

const schema = z.object({
  name:        z.string().min(1, "Name is required"),
  sku:         z.string().min(1, "SKU is required"),
  description: z.string().min(1, "Description is required"),
});

type Schema = z.infer<typeof schema>;

export default function ProductFormModal() {
  const dispatch     = useAppDispatch();
  const activeModal  = useAppSelector(selectActiveModal);
  const modalPayload = useAppSelector(selectModalPayload);
  const queryClient  = useQueryClient();

  const isCreate = activeModal === "createProduct";
  const isEdit   = activeModal === "editProduct";
  const isOpen   = isCreate || isEdit;
  const editId   = isEdit ? modalPayload : null;

  const { data: existing } = useQuery({
    queryKey: ["product", editId],
    queryFn:  () => productsService.getById(editId!),
    enabled:  !!editId,
  });

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Schema>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", sku: "", description: "" },
  });

  useEffect(() => {
    if (isOpen && existing) {
      reset({ name: existing.name, sku: existing.sku, description: existing.description });
    } else if (isOpen && isCreate) {
      reset({ name: "", sku: "", description: "" });
    }
  }, [isOpen, isCreate, existing, reset]);

  const onClose = () => dispatch(closeModal());

  const mutation = useMutation({
    mutationFn: (data: Schema) =>
      editId
        ? productsService.update(editId, data)
        : productsService.create(data),
    onSuccess: () => {
      toast.success(editId ? "Product updated." : "Product created.");
      queryClient.invalidateQueries({ queryKey: ["products"] });
      onClose();
    },
    onError: () => toast.error("Failed to save product."),
  });

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isCreate ? "New Product" : "Edit Product"}>
      <form onSubmit={handleSubmit((d) => mutation.mutate(d))}>
        <ModalBody>
          <Input
            label="Name"
            placeholder="Product name"
            error={errors.name?.message}
            {...register("name")}
          />
          <Input
            label="SKU"
            placeholder="SKU-001"
            error={errors.sku?.message}
            {...register("sku")}
          />
          <Input
            label="Description"
            placeholder="Brief product description"
            error={errors.description?.message}
            {...register("description")}
          />
          {mutation.isError && (
            <span style={{ color: "red", fontSize: "0.8rem" }}>
              Failed to save product. Please try again.
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
