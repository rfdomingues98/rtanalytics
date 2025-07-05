import {faker} from '@faker-js/faker'
import {Kafka, type Producer} from 'kafkajs'
import {config, updateConfig} from './config.js'
import {generateEventData} from './event-data.js'
import {getLogger} from './logger.js'
import type {GeneratorStats, User} from './types.js'
import {eventTypes} from './types.js'

export class EventGenerator {
  private isRunning = false
  private users: User[] = []
  private activeGenerators: NodeJS.Timeout[] = []

  // Single Kafka producer shared across all users for efficiency
  private kafkaProducer: Producer | null = null
  private kafkaConnected = false

  constructor(initialUserCount: number = config.userCount) {
    updateConfig({userCount: initialUserCount})
    this.initializeUsers()
    this.initializeKafka()
  }

  private async initializeKafka() {
    if (!config.kafka.enabled) {
      return
    }

    const logger = getLogger()
    try {
      // Create single Kafka client and producer for all users
      const client = new Kafka({
        clientId: config.kafka.clientId,
        brokers: config.kafka.brokers,
      })

      this.kafkaProducer = client.producer({
        // Optimize for high throughput scenarios
        maxInFlightRequests: 5,
        idempotent: true,
        transactionTimeout: 30000,
      })
      logger.info('Kafka producer initialized (shared across all users)')
    } catch (error) {
      logger.error({error}, 'Failed to initialize Kafka producer')
    }
  }

  private async connectKafka() {
    if (!config.kafka.enabled || !this.kafkaProducer || this.kafkaConnected) {
      return
    }

    const logger = getLogger()
    try {
      await this.kafkaProducer.connect()
      this.kafkaConnected = true
      logger.info('Connected to Kafka')
    } catch (error) {
      logger.error({error}, 'Failed to connect to Kafka')
    }
  }

  private async disconnectKafka() {
    if (!this.kafkaProducer || !this.kafkaConnected) {
      return
    }

    const logger = getLogger()
    try {
      await this.kafkaProducer.disconnect()
      this.kafkaConnected = false
      logger.info('Disconnected from Kafka')
    } catch (error) {
      logger.error({error}, 'Failed to disconnect from Kafka')
    }
  }

  private initializeUsers() {
    this.users = Array.from({length: config.userCount}, (_, index) => ({
      id: `user_${faker.string.alphanumeric(8)}_${index
        .toString()
        .padStart(3, '0')}`,
      sessionId: faker.string.uuid(),
      isActive: true,
    }))
  }

  public async startGenerating(): Promise<boolean> {
    if (this.isRunning) {
      return false
    }

    // Connect to Kafka if enabled
    await this.connectKafka()

    this.isRunning = true
    const logger = getLogger()
    logger.info(`Starting event generation for ${config.userCount} users`)
    logger.info(
      `Event interval range: ${config.baseEventInterval}ms - ${config.maxEventInterval}ms`
    )
    logger.info(`Kafka enabled: ${config.kafka.enabled}`)

    for (const user of this.users) {
      if (user.isActive) {
        this.startUserEventGenerator(user)
      }
    }
    return true
  }

  private startUserEventGenerator(user: User): void {
    const generateEvent = async () => {
      if (!this.isRunning || !user.isActive) return

      const event = this.generateRandomEvent(user)
      const logger = getLogger()

      // Send to Kafka if enabled and connected
      if (config.kafka.enabled && this.kafkaConnected && this.kafkaProducer) {
        try {
          await this.kafkaProducer.send({
            topic: config.kafka.topic,
            messages: [
              {
                key: user.id,
                value: JSON.stringify(event),
                timestamp: Date.now().toString(),
              },
            ],
          })
          logger.info(
            {event, userId: user.id, sink: 'kafka'},
            'Event sent to Kafka'
          )
        } catch (error) {
          logger.error(
            {error, event, userId: user.id},
            'Failed to send event to Kafka'
          )
          // Still log to file as fallback
          logger.info(
            {event, userId: user.id, sink: 'file'},
            'Event logged to file'
          )
        }
      } else {
        // Log to file only
        logger.info(
          {event, userId: user.id, sink: 'file'},
          'Event logged to file'
        )
      }

      const nextInterval = faker.number.int({
        min: config.baseEventInterval,
        max: config.maxEventInterval,
      })

      const timeoutId = setTimeout(generateEvent, nextInterval)
      this.activeGenerators.push(timeoutId)
    }

    generateEvent()
  }

  public async stopGenerating(): Promise<boolean> {
    if (!this.isRunning) {
      return false
    }

    this.isRunning = false
    this.activeGenerators.forEach(clearTimeout)
    this.activeGenerators = []

    const logger = getLogger()
    logger.info('Event generator stopped')

    // Disconnect from Kafka
    await this.disconnectKafka()

    return true
  }

  public updateUserCount(newUserCount: number): boolean {
    if (newUserCount < 1) {
      return false
    }

    const currentCount = this.users.filter((u) => u.isActive).length
    const logger = getLogger()

    if (newUserCount > currentCount) {
      const usersToAdd = newUserCount - currentCount
      logger.info(`Scaling up: adding ${usersToAdd} users`)

      // Reactivate existing inactive users first
      const inactiveUsers = this.users.filter((u) => !u.isActive)
      for (let i = 0; i < Math.min(usersToAdd, inactiveUsers.length); i++) {
        inactiveUsers[i].isActive = true
        inactiveUsers[i].sessionId = faker.string.uuid()
        if (this.isRunning) {
          this.startUserEventGenerator(inactiveUsers[i])
        }
      }

      // Create new users if needed
      const stillNeed = usersToAdd - inactiveUsers.length
      if (stillNeed > 0) {
        const startIndex = this.users.length
        for (let i = 0; i < stillNeed; i++) {
          const newUser: User = {
            id: `user_${faker.string.alphanumeric(8)}_${(startIndex + i)
              .toString()
              .padStart(3, '0')}`,
            sessionId: faker.string.uuid(),
            isActive: true,
          }
          this.users.push(newUser)
          if (this.isRunning) {
            this.startUserEventGenerator(newUser)
          }
        }
      }
    } else if (newUserCount < currentCount) {
      // Deactivate users
      const usersToDeactivate = currentCount - newUserCount
      logger.info(`Scaling down: deactivating ${usersToDeactivate} users`)

      const activeUsers = this.users.filter((u) => u.isActive)
      for (let i = 0; i < usersToDeactivate; i++) {
        activeUsers[i].isActive = false
      }
    }

    updateConfig({userCount: newUserCount})
    const activeCount = this.users.filter((u) => u.isActive).length
    logger.info(`User count updated to ${activeCount}`)
    return true
  }

  public getStats(): GeneratorStats {
    const activeUsers = this.users.filter((u) => u.isActive).length
    const totalUsers = this.users.length

    return {
      activeUsers,
      totalUsers,
      isRunning: this.isRunning,
      config,
    }
  }

  private generateRandomEvent(user: User) {
    const eventType = eventTypes[Math.floor(Math.random() * eventTypes.length)]
    return generateEventData(eventType, user)
  }

  // Cleanup method for graceful shutdown
  public async cleanup() {
    await this.stopGenerating()
  }
}
