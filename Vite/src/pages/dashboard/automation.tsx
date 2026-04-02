import { Button, Card, Chip } from '@heroui/react';
import { useNavigate } from 'react-router-dom';

const backlog = [
  'If this, then that для комнат и устройств.',
  'Триггеры по температуре, времени и статусу сети.',
  'B2B-сценарии для офисов и малых пространств.',
];

const AutomationPage = () => {
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <Card className="rt-card overflow-hidden">
        <Card.Content className="grid gap-6 p-6 md:p-8 xl:grid-cols-[1fr_auto] xl:items-center">
          <div>
            <div className="rt-kicker">Automation</div>
            <h1 className="rt-display mt-3 text-4xl font-semibold">Автоматизации — следующий шаг после BLE MVP.</h1>
            <p className="mt-4 max-w-3xl text-sm leading-relaxed text-[color:var(--muted)] md:text-base">
              На хакатоне мы сознательно вложились в auth, API и устройство-первый опыт. Сценарии уже спроектированы как следующий модуль.
            </p>
          </div>
          <Button className="rt-btn rt-btn-primary" onClick={() => navigate('/dashboard')}>
            Вернуться к устройствам
          </Button>
        </Card.Content>
      </Card>

      <div className="grid gap-4 lg:grid-cols-3">
        {backlog.map((item, index) => (
          <Card key={item} className="rt-card-soft">
            <Card.Content className="p-5">
              <Chip className="rt-pill text-xs">0{index + 1}</Chip>
              <div className="mt-4 text-lg font-semibold">{item}</div>
            </Card.Content>
          </Card>
        ))}
      </div>
    </div>
  );
};

export default AutomationPage;
