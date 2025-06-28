import {faker} from '@faker-js/faker'
import {config, updateConfig} from './config.js'
import {generateEventData} from './event-data.js'
import {getLogger} from './logger.js'
import type {GeneratorStats, User} from './types.js'
import {eventTypes} from './types.js'

export class EventGenerator {
  private isRunning = false
  private users: User[] = []
  private activeGenerators: NodeJS.Timeout[] = []

  constructor(initialUserCount: number = config.userCount) {
    updateConfig({userCount: initialUserCount})
    this.initializeUsers()
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

    this.isRunning = true
    const logger = getLogger()
    logger.info(`Starting event generation for ${config.userCount} users`)
    logger.info(
      `Event interval range: ${config.baseEventInterval}ms - ${config.maxEventInterval}ms`
    )

    for (const user of this.users) {
      if (user.isActive) {
        this.startUserEventGenerator(user)
      }
    }
    return true
  }

  private startUserEventGenerator(user: User): void {
    const generateEvent = () => {
      if (!this.isRunning || !user.isActive) return

      const event = this.generateRandomEvent(user)
      const logger = getLogger()
      logger.info({event, userId: user.id}, 'Generated event')

      const nextInterval = faker.number.int({
        min: config.baseEventInterval,
        max: config.maxEventInterval,
      })

      const timeoutId = setTimeout(generateEvent, nextInterval)
      this.activeGenerators.push(timeoutId)
    }

    generateEvent()
  }

  public stopGenerating(): boolean {
    if (!this.isRunning) {
      return false
    }

    this.isRunning = false
    this.activeGenerators.forEach(clearTimeout)
    this.activeGenerators = []

    const logger = getLogger()
    logger.info('Event generator stopped')
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
}
