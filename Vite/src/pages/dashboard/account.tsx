import { Button, Card, Chip, toast } from '@heroui/react';
import { useMemo, useState } from 'react';
import { useAuth } from '../../hooks/auth';

function formatDate(value: string) {
  try {
    return new Intl.DateTimeFormat('ru-RU', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  } catch {
    return value;
  }
}

const AccountPage = () => {
  const { user, reloadUser, revokeSession, logout } = useAuth();
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const stats = useMemo(() => {
    const sessions = user?.activeSessions ?? [];
    return {
      total: sessions.length,
      current: sessions.filter((session) => session.isCurrent).length,
      recent: sessions.filter((session) => Date.now() - new Date(session.lastUsed).getTime() < 1000 * 60 * 60 * 24).length,
    };
  }, [user?.activeSessions]);

  const handleRefresh = async () => {
    setIsRefreshing(true);
    try {
      await reloadUser();
      toast.success('Профиль обновлён.');
    } catch (error) {
      console.error(error);
      toast.danger('Не удалось обновить данные профиля.');
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleRevoke = async (sessionId: string) => {
    setRevokingId(sessionId);
    const result = await revokeSession(sessionId);
    setRevokingId(null);

    if (!result.success) {
      toast.danger(result.error);
      return;
    }

    toast.success('Сессия отозвана.');
  };

  if (!user) {
    return <Card className="rt-card p-8 text-sm text-[color:var(--muted)]">Профиль ещё загружается...</Card>;
  }

  return (
    <div className="space-y-6">
      <Card className="rt-card overflow-hidden">
        <Card.Content className="grid gap-6 p-6 md:p-8 xl:grid-cols-[1.1fr_0.9fr] xl:items-end">
          <div>
            <div className="rt-kicker">Account</div>
            <h1 className="rt-display mt-3 text-4xl font-semibold">Управление сессиями и доступом.</h1>
            <p className="mt-4 max-w-3xl text-sm leading-relaxed text-[color:var(--muted)] md:text-base">
              Профиль привязан к backend auth flow: JWT, refresh token и список активных сессий пользователя.
            </p>
            <div className="mt-5 flex flex-wrap gap-2">
              <Chip className="rt-pill text-xs">{user.email}</Chip>
              <Chip className="rt-pill text-xs">userId: {user.userId}</Chip>
            </div>
          </div>

          <div className="flex flex-wrap gap-3 xl:justify-end">
            <Button className="rt-btn rt-btn-secondary" onClick={() => void handleRefresh()} isDisabled={isRefreshing}>
              {isRefreshing ? 'Обновляем...' : 'Обновить профиль'}
            </Button>
            <Button className="rt-btn rt-btn-primary" onClick={() => void logout()}>
              Выйти из аккаунта
            </Button>
          </div>
        </Card.Content>
      </Card>

      <div className="grid gap-4 md:grid-cols-3">
        <Card className="rt-card-soft"><Card.Content className="p-5"><div className="text-sm text-[color:var(--muted)]">Всего сессий</div><div className="mt-2 text-3xl font-semibold">{stats.total}</div></Card.Content></Card>
        <Card className="rt-card-soft"><Card.Content className="p-5"><div className="text-sm text-[color:var(--muted)]">Текущих устройств</div><div className="mt-2 text-3xl font-semibold">{stats.current}</div></Card.Content></Card>
        <Card className="rt-card-soft"><Card.Content className="p-5"><div className="text-sm text-[color:var(--muted)]">Активность за 24ч</div><div className="mt-2 text-3xl font-semibold">{stats.recent}</div></Card.Content></Card>
      </div>

      <div className="grid gap-4">
        {user.activeSessions.map((session) => (
          <Card key={session.sessionId} className="rt-card-soft overflow-hidden">
            <Card.Content className="flex flex-col gap-5 p-5 lg:flex-row lg:items-center lg:justify-between">
              <div className="space-y-2">
                <div className="flex flex-wrap items-center gap-2">
                  <div className="text-lg font-semibold">{session.deviceInfo}</div>
                  {session.isCurrent ? <Chip className="rt-pill text-xs">Текущая сессия</Chip> : null}
                </div>
                <div className="text-sm text-[color:var(--muted)]">IP: {session.ipAddress}</div>
                <div className="grid gap-2 text-sm text-[color:var(--muted)] md:grid-cols-3">
                  <div>Создана: {formatDate(session.createdAt)}</div>
                  <div>Последняя активность: {formatDate(session.lastUsed)}</div>
                  <div>Истекает: {formatDate(session.expiresAt)}</div>
                </div>
              </div>

              <div className="flex flex-wrap gap-3">
                <Button className="rt-btn rt-btn-secondary" onClick={() => navigator.clipboard?.writeText(session.sessionId)}>
                  Скопировать ID
                </Button>
                <Button
                  className="rt-btn rt-btn-primary"
                  onClick={() => void handleRevoke(session.sessionId)}
                  isDisabled={session.isCurrent || revokingId === session.sessionId}
                >
                  {revokingId === session.sessionId ? 'Отзываем...' : 'Отозвать'}
                </Button>
              </div>
            </Card.Content>
          </Card>
        ))}
      </div>
    </div>
  );
};

export default AccountPage;
