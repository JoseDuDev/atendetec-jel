import { useState } from 'react';
import { useCreateWhatsAppAccount, useWhatsAppAccounts } from '@/hooks/useWhatsApp';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Plus } from 'lucide-react';

type Provider = 'meta' | 'evolution';

const CONFIG_PLACEHOLDER: Record<Provider, string> = {
  meta: JSON.stringify({ phoneNumberId: '1234567890', accessToken: 'EAAxxxxxxx' }, null, 2),
  evolution: JSON.stringify(
    { base_url: 'http://evolution-api:8080', instance: 'atendefy-dev', api_key: 'dev_evolution_key' },
    null,
    2
  ),
};

export default function WhatsAppPage() {
  const { data: accounts, isLoading } = useWhatsAppAccounts();
  const createAccount = useCreateWhatsAppAccount();

  const [open, setOpen] = useState(false);
  const [provider, setProvider] = useState<Provider>('meta');
  const [phone, setPhone] = useState('');
  const [configJson, setConfigJson] = useState(CONFIG_PLACEHOLDER.meta);
  const [error, setError] = useState('');

  function handleProviderChange(v: string) {
    const p = v as Provider;
    setProvider(p);
    setConfigJson(CONFIG_PLACEHOLDER[p]);
  }

  async function handleCreate() {
    setError('');
    try {
      JSON.parse(configJson);
    } catch {
      setError('configJson inválido — verifique o JSON.');
      return;
    }
    try {
      await createAccount.mutateAsync({ provider, phone, configJson });
      setOpen(false);
      setPhone('');
      setConfigJson(CONFIG_PLACEHOLDER[provider]);
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        'Erro ao criar conta.';
      setError(msg);
    }
  }

  function statusVariant(status: string): 'default' | 'secondary' | 'outline' {
    if (status === 'connected') return 'default';
    if (status === 'disconnected') return 'secondary';
    return 'outline';
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Contas WhatsApp</h1>
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger render={<Button />}>
            <Plus className="h-4 w-4 mr-2" />
            Nova conta
          </DialogTrigger>
          <DialogContent className="max-w-lg">
            <DialogHeader>
              <DialogTitle>Conectar conta WhatsApp</DialogTitle>
            </DialogHeader>
            <div className="space-y-4">
              <div className="space-y-1">
                <Label>Provedor</Label>
                <Select value={provider} onValueChange={(v) => v && handleProviderChange(v)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="meta">Meta (WhatsApp Cloud API)</SelectItem>
                    <SelectItem value="evolution">Evolution API</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="phone">Número (com DDI, ex: +5511999999999)</Label>
                <Input
                  id="phone"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="+5511999999999"
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="configJson">Configuração (JSON)</Label>
                <Textarea
                  id="configJson"
                  className="font-mono text-xs"
                  rows={6}
                  value={configJson}
                  onChange={(e) => setConfigJson(e.target.value)}
                />
              </div>
              {error && <p className="text-sm text-destructive">{error}</p>}
              <Button
                className="w-full"
                onClick={handleCreate}
                disabled={createAccount.isPending}
              >
                {createAccount.isPending ? 'Salvando…' : 'Salvar'}
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      {isLoading && <p className="text-muted-foreground">Carregando…</p>}

      {!isLoading && accounts?.length === 0 && (
        <p className="text-muted-foreground">Nenhuma conta conectada ainda.</p>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {accounts?.map((acc) => (
          <Card key={acc.id}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium capitalize">{acc.provider}</CardTitle>
              <Badge variant={statusVariant(acc.status)}>{acc.status}</Badge>
            </CardHeader>
            <CardContent>
              <p className="text-sm">{acc.phone}</p>
              <p className="text-xs text-muted-foreground mt-1">
                {new Date(acc.createdAt).toLocaleDateString('pt-BR')}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
