import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { apiClient } from '@/api/client';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Bot, CreditCard, MessageSquare, Wifi } from 'lucide-react';

function useApiHealth() {
  return useQuery({
    queryKey: ['health'],
    queryFn: () =>
      apiClient.get<{ status: string }>('/health').then((r) => r.data),
  });
}

export default function DashboardPage() {
  const { data: health, isError } = useApiHealth();

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <Badge variant={isError ? 'destructive' : 'default'}>
          API {isError ? 'offline' : (health?.status ?? '…')}
        </Badge>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricCard
          title="WhatsApp"
          description="Conecte contas e receba mensagens"
          icon={<Wifi className="h-5 w-5" />}
          to="/whatsapp"
          linkLabel="Gerenciar"
        />
        <MetricCard
          title="IA"
          description="Configure provedor e system prompt"
          icon={<Bot className="h-5 w-5" />}
          to="/ai-config"
          linkLabel="Configurar"
        />
        <MetricCard
          title="Conversas"
          description="Histórico de atendimentos"
          icon={<MessageSquare className="h-5 w-5" />}
          to="/conversations"
          linkLabel="Ver"
        />
        <MetricCard
          title="Billing"
          description="Planos e assinaturas"
          icon={<CreditCard className="h-5 w-5" />}
          to="/billing"
          linkLabel="Gerenciar"
        />
      </div>
    </div>
  );
}

function MetricCard({
  title,
  description,
  icon,
  to,
  linkLabel,
}: {
  title: string;
  description: string;
  icon: React.ReactNode;
  to: string;
  linkLabel: string;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        {icon}
      </CardHeader>
      <CardContent>
        <p className="text-xs text-muted-foreground mb-3">{description}</p>
        <Button size="sm" variant="outline" render={<Link to={to} />}>
          {linkLabel}
        </Button>
      </CardContent>
    </Card>
  );
}
