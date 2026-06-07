import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/api/client';
import type {
  CreateWhatsAppAccountRequest,
  WhatsAppAccount,
  WhatsAppConnectResponse,
  WhatsAppStatusResponse,
} from '@/types/api';

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

export function useConnectWhatsApp() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient
        .post<WhatsAppConnectResponse>(`/whatsapp/accounts/${id}/connect`)
        .then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['whatsapp-accounts'] }),
  });
}

export function useWhatsAppStatus(id: string | null, enabled: boolean) {
  const queryClient = useQueryClient();
  return useQuery({
    queryKey: ['whatsapp-status', id],
    queryFn: () =>
      apiClient
        .get<WhatsAppStatusResponse>(`/whatsapp/accounts/${id}/status`)
        .then((r) => r.data),
    enabled: !!id && enabled,
    refetchInterval: (query) =>
      query.state.data?.status === 'open' ? false : 3000,
    select: (data) => {
      if (data.status === 'open') {
        queryClient.invalidateQueries({ queryKey: ['whatsapp-accounts'] });
      }
      return data;
    },
  });
}
