import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/api/client';
import type { CreateWhatsAppAccountRequest, WhatsAppAccount } from '@/types/api';

export function useWhatsAppAccounts() {
  return useQuery({
    queryKey: ['whatsapp-accounts'],
    queryFn: () =>
      apiClient.get<WhatsAppAccount[]>('/whatsapp/accounts').then((r) => r.data),
  });
}

export function useCreateWhatsAppAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateWhatsAppAccountRequest) =>
      apiClient.post<WhatsAppAccount>('/whatsapp/accounts', req).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['whatsapp-accounts'] }),
  });
}
