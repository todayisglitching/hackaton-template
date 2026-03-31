import './EmptyState.css'

export function EmptyState() {
  return (
    <div className="empty">
      <h2>Нет устройств</h2>
      <p>Добавьте чайник, чтобы начать получать температуру.</p>
    </div>
  )
}
