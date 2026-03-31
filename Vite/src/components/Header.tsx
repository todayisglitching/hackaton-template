import './Header.css'

export function Header() {
  return (
    <header className="app__hero">
      <div>
        <p className="app__eyebrow">Ростелеком • Smart Hub</p>
        <h1>Контроль температуры чайника</h1>
        <p className="app__sub">
          Интерфейс подключается к BLE-сервису `1234` и отправляет показания в облако через ASP.NET API.
        </p>
      </div>
      <div className="app__badge">BLE • WebSocket • ASP.NET</div>
    </header>
  )
}
