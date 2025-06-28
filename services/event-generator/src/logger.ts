import pino from 'pino'
import {config} from './config.js'

let logger: pino.Logger

export function createLogger(logFilePath?: string): pino.Logger {
  const dest = logFilePath || config.logFile

  logger = pino(
    {
      name: 'Event Generator',
      level: 'info',
    },
    pino.destination({
      dest,
      sync: false,
    })
  )

  return logger
}

export function getLogger(): pino.Logger {
  if (!logger) {
    throw new Error('Logger not initialized. Call createLogger() first.')
  }
  return logger
}

export function updateLoggerDestination(newPath: string): void {
  logger = createLogger(newPath)
}
