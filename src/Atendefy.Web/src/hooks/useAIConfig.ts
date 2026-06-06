import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/api/client';
import type { AIConfigRequest, AIConfigResponse } from '@/types/api';

export function useAIConfig() {
  return useQuery({
    queryKey: ['ai-config'],
    queryFn: () =>
      apiClient
        .get<AIConfigResponse>('/ai/config')
        .then((r) => r.data)
        .catch((err: { response?: { status?: number } }) => {
          if (err?.response?.status === 404) return null;
          throw err;
        }),
  });
}

export function useSaveAIConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (req: AIConfigRequest) =>
      apiClient.put<AIConfigResponse>('/ai/config', req).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ai-config'] }),
  });
}
