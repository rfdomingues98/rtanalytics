import {Command} from 'commander'
import * as path from 'path'
import {updateConfig} from '../config.js'
import {EventGenerator} from '../event-generator.js'
import {createLogger} from '../logger.js'
import type {InteractiveOptions, StartOptions} from '../types.js'
import {startInteractiveMode} from './interactive.js'

export function setupCommands(): Command {
  const program = new Command()

  program
    .name('event-generator')
    .description('CLI tool for generating analytics events')
    .version('1.0.0')

  program
    .command('interactive')
    .alias('i')
    .description('Start interactive mode')
    .option('-u, --users <number>', 'Initial number of users', '10')
    .option('-l, --log-file <path>', 'Log file path', 'events.log')
    .option('--min-interval <ms>', 'Minimum time between events (ms)', '2000')
    .option('--max-interval <ms>', 'Maximum time between events (ms)', '5000')
    .action(async (options: InteractiveOptions) => {
      const logFile = path.resolve(options.logFile)
      const configUpdates: any = {logFile}

      if (options.minInterval) {
        configUpdates.baseEventInterval = parseInt(options.minInterval)
      }
      if (options.maxInterval) {
        configUpdates.maxEventInterval = parseInt(options.maxInterval)
      }

      updateConfig(configUpdates)
      createLogger(logFile)

      const generator = new EventGenerator(parseInt(options.users))

      console.log('🚀 Event Generator CLI')
      console.log(`Log file: ${logFile}`)
      console.log(
        `Event intervals: ${configUpdates.baseEventInterval || 2000}ms - ${
          configUpdates.maxEventInterval || 5000
        }ms`
      )
      console.log('Type "help" for available commands\n')

      startInteractiveMode(generator)
    })

  program
    .command('start')
    .description('Start generating events')
    .option('-u, --users <number>', 'Number of users', '10')
    .option('-l, --log-file <path>', 'Log file path', 'events.log')
    .option('--min-interval <ms>', 'Minimum time between events (ms)', '2000')
    .option('--max-interval <ms>', 'Maximum time between events (ms)', '5000')
    .action(async (options: StartOptions) => {
      const logFile = path.resolve(options.logFile)
      const configUpdates: any = {logFile}

      if (options.minInterval) {
        configUpdates.baseEventInterval = parseInt(options.minInterval)
      }
      if (options.maxInterval) {
        configUpdates.maxEventInterval = parseInt(options.maxInterval)
      }

      updateConfig(configUpdates)
      createLogger(logFile)

      const generator = new EventGenerator(parseInt(options.users))

      // Graceful shutdown handler
      const cleanup = async () => {
        console.log('\n🛑 Shutting down...')
        await generator.cleanup()
        process.exit(0)
      }

      process.on('SIGINT', cleanup)
      process.on('SIGTERM', cleanup)

      const started = await generator.startGenerating()
      if (started) {
        console.log(`✅ Started generating events with ${options.users} users`)
        console.log(`📁 Logging to: ${logFile}`)
        console.log(`⚡ Kafka enabled: true`)
        console.log(
          `🔄 Event intervals: ${configUpdates.baseEventInterval || 2000}ms - ${
            configUpdates.maxEventInterval || 5000
          }ms`
        )
        console.log('\n💡 Press Ctrl+C to stop')
      } else {
        console.log('⚠️  Generator is already running')
      }
    })

  return program
}
