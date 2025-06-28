#!/usr/bin/env node

import {setupCommands} from './cli/commands.js'

// Parse CLI arguments
const program = setupCommands()

if (process.argv.length <= 2) {
  // No arguments provided, start interactive mode
  program.parse(['node', 'event-generator', 'interactive'])
} else {
  program.parse()
}

// Export for potential external use
export {EventGenerator} from './event-generator.js'
export type {
  EventData,
  EventType,
  GeneratorConfig,
  GeneratorStats,
  User,
} from './types.js'
