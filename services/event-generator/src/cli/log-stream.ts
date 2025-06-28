import {spawn} from 'child_process'
import * as fs from 'fs'
import {config} from '../config.js'

let logProcess: any = null

export function startLogStream(): void {
  if (logProcess) {
    console.log('⚠️  Log streaming is already active')
    return
  }

  if (!fs.existsSync(config.logFile)) {
    fs.writeFileSync(config.logFile, '')
  }

  console.log('📡 Starting log stream...')

  logProcess = spawn('tail', ['-f', config.logFile], {
    stdio: ['pipe', 'pipe', 'pipe'],
  })

  logProcess.stdout.on('data', (data: Buffer) => {
    const lines = data
      .toString()
      .split('\n')
      .filter((line) => line.trim())
    lines.forEach((line) => {
      try {
        const logEntry = JSON.parse(line)
        const timestamp = new Date(logEntry.time).toLocaleTimeString()
        const userId = logEntry.userId ? `[${logEntry.userId}]` : ''
        console.log(`${timestamp} INFO ${userId} ${logEntry.msg}`)
      } catch {
        console.log(line)
      }
    })
  })

  logProcess.on('error', (err: Error) => {
    console.log(`❌ Log stream error: ${err.message}`)
    logProcess = null
  })

  logProcess.on('close', () => {
    logProcess = null
  })
}

export function stopLogStream(): void {
  if (!logProcess) {
    console.log('⚠️  Log streaming is not active')
    return
  }

  logProcess.kill()
  logProcess = null
  console.log('✅ Log streaming stopped')
}

export function cleanup(): void {
  if (logProcess) {
    logProcess.kill()
  }
}
