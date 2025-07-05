import * as readline from 'node:readline'
import {config, updateConfig} from '../config.js'
import type {EventGenerator} from '../event-generator.js'
import {cleanup, startLogStream, stopLogStream} from './log-stream.js'

export function startInteractiveMode(generator: EventGenerator): void {
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: 'event-gen> ',
  })

  rl.prompt()

  rl.on('line', async (line) => {
    const [command, ...args] = line.trim().split(' ')

    switch (command.toLowerCase()) {
      case 'help':
        showHelp()
        break

      case 'start':
        if (!generator) {
          console.log('❌ Generator not initialized')
          break
        }
        const started = await generator.startGenerating()
        console.log(
          started
            ? '✅ Event generation started'
            : '⚠️  Generator is already running'
        )
        break

      case 'stop':
        if (!generator) {
          console.log('❌ Generator not initialized')
          break
        }
        const stopped = await generator.stopGenerating()
        console.log(
          stopped
            ? '✅ Event generation stopped'
            : '⚠️  Generator is not running'
        )
        break

      case 'users':
        if (!generator) {
          console.log('❌ Generator not initialized')
          break
        }

        if (args.length === 0) {
          const stats = generator.getStats()
          console.log(`👥 Active users: ${stats.activeUsers}`)
        } else {
          const userCount = parseInt(args[0])
          if (isNaN(userCount) || userCount < 1) {
            console.log('❌ Invalid user count')
          } else {
            const updated = generator.updateUserCount(userCount)
            console.log(
              updated
                ? `✅ User count updated to ${userCount}`
                : '❌ Failed to update user count'
            )
          }
        }
        break

      case 'interval':
        if (args.length === 2) {
          const min = parseInt(args[0])
          const max = parseInt(args[1])
          if (isNaN(min) || isNaN(max) || min < 100 || max < min) {
            console.log(
              '❌ Invalid intervals. Min must be >= 100ms and max must be >= min'
            )
          } else {
            updateConfig({baseEventInterval: min, maxEventInterval: max})
            console.log(`✅ Intervals updated: ${min}ms - ${max}ms`)
          }
        } else {
          console.log(
            `⚙️  Current intervals: ${config.baseEventInterval}ms - ${config.maxEventInterval}ms`
          )
        }
        break

      case 'min-interval':
        if (args.length === 1) {
          const min = parseInt(args[0])
          if (isNaN(min) || min < 100) {
            console.log('❌ Invalid minimum interval. Must be >= 100ms')
          } else if (min > config.maxEventInterval) {
            console.log(
              '❌ Minimum interval cannot be greater than maximum interval'
            )
          } else {
            updateConfig({baseEventInterval: min})
            console.log(`✅ Minimum interval updated to ${min}ms`)
          }
        } else {
          console.log(
            `⚙️  Current minimum interval: ${config.baseEventInterval}ms`
          )
        }
        break

      case 'max-interval':
        if (args.length === 1) {
          const max = parseInt(args[0])
          if (isNaN(max) || max < 100) {
            console.log('❌ Invalid maximum interval. Must be >= 100ms')
          } else if (max < config.baseEventInterval) {
            console.log(
              '❌ Maximum interval cannot be less than minimum interval'
            )
          } else {
            updateConfig({maxEventInterval: max})
            console.log(`✅ Maximum interval updated to ${max}ms`)
          }
        } else {
          console.log(
            `⚙️  Current maximum interval: ${config.maxEventInterval}ms`
          )
        }
        break

      case 'stats':
        if (!generator) {
          console.log('❌ Generator not initialized')
          break
        }
        const stats = generator.getStats()
        console.log('📊 Generator Stats:')
        console.log(`   Status: ${stats.isRunning ? 'Running' : 'Stopped'}`)
        console.log(`   Active Users: ${stats.activeUsers}`)
        console.log(`   Total Users: ${stats.totalUsers}`)
        console.log(
          `   Intervals: ${config.baseEventInterval}ms - ${config.maxEventInterval}ms`
        )
        console.log(
          `   Kafka: ${config.kafka.enabled ? 'Enabled' : 'Disabled'}`
        )
        console.log(`   Kafka Topic: ${config.kafka.topic}`)
        console.log(`   Log File: ${config.logFile}`)
        break

      case 'logs':
        if (args[0] === 'start') {
          startLogStream()
        } else if (args[0] === 'stop') {
          stopLogStream()
        } else {
          console.log('Usage: logs start|stop')
        }
        break

      case 'clear':
        console.clear()
        break

      case 'exit':
      case 'quit':
        console.log('👋 Goodbye!')
        cleanup()
        if (generator) {
          await generator.cleanup()
        }
        process.exit(0)

      default:
        if (command) {
          console.log(`❌ Unknown command: ${command}`)
          console.log('Type "help" for available commands')
        }
        break
    }

    rl.prompt()
  })

  rl.on('close', () => {
    console.log('\n👋 Goodbye!')
    cleanup()
    if (generator) {
      generator.cleanup()
    }
    process.exit(0)
  })
}

function showHelp() {
  console.log('\n📖 Available Commands:')
  console.log('  start               - Start generating events')
  console.log('  stop                - Stop generating events')
  console.log('  users [number]      - Show or set number of active users')
  console.log('  interval <min> <max> - Set event timing intervals (ms)')
  console.log('  min-interval [ms]   - Show or set minimum interval')
  console.log('  max-interval [ms]   - Show or set maximum interval')
  console.log('  stats               - Show generator statistics')
  console.log('  logs start          - Start streaming logs to terminal')
  console.log('  logs stop           - Stop streaming logs')
  console.log('  clear               - Clear terminal')
  console.log('  help                - Show this help message')
  console.log('  exit                - Exit the program')
  console.log()
}
