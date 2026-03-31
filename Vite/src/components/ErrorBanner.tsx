import { Alert } from '@heroui/react'

export function ErrorBanner({ message }: { message: string | null }) {
  if (!message) return null
  
  return (
    <Alert status="danger" className="w-full">
      <Alert.Indicator />
      <Alert.Content>
        <Alert.Title>Ошибка</Alert.Title>
        <Alert.Description>{message}</Alert.Description>
      </Alert.Content>
    </Alert>
  )
}
