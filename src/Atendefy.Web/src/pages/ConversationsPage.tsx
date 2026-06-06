import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MessageSquare } from 'lucide-react';

export default function ConversationsPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Conversas</h1>
      <Card className="max-w-md">
        <CardHeader>
          <div className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5" />
            <CardTitle>Em breve</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            O histórico de conversas por contato será implementado no Plano 5. As mensagens já
            estão sendo processadas e armazenadas pelo ConversationWorker no backend.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
