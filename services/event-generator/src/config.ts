import * as path from 'path'
import type {GeneratorConfig} from './types.js'

export const defaultConfig: GeneratorConfig = {
  userCount: 10,
  baseEventInterval: 2000,
  maxEventInterval: 5000,
  logFile: path.join(process.cwd(), 'events.log'),
}

// Global config instance
export let config = {...defaultConfig}

export function updateConfig(updates: Partial<GeneratorConfig>): void {
  config = {...config, ...updates}
}

export function resetConfig(): void {
  config = {...defaultConfig}
}

export function getConfig(): GeneratorConfig {
  return {...config}
}
