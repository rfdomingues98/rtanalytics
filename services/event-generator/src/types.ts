export const eventTypes = [
  'click',
  'page_view',
  'purchase',
  'add_to_cart',
  'checkout',
  'favorite',
  'add_review',
] as const

export type EventType = (typeof eventTypes)[number]

export type EventData = {
  event_type: EventType
  user_id: string
  timestamp: number
  metadata: Record<string, unknown>
}

export type User = {
  id: string
  sessionId: string
  isActive: boolean
}

export type GeneratorStats = {
  activeUsers: number
  totalUsers: number
  isRunning: boolean
  config: GeneratorConfig
}

export type GeneratorConfig = {
  userCount: number
  baseEventInterval: number
  maxEventInterval: number
  logFile: string
}

export interface InteractiveOptions {
  users: string
  logFile: string
  minInterval?: string
  maxInterval?: string
}

export interface StartOptions {
  users: string
  logFile: string
  minInterval?: string
  maxInterval?: string
}
