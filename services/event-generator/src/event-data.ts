import {faker} from '@faker-js/faker'
import type {EventData, EventType, User} from './types.js'

// Set a fixed seed for predictable results
faker.seed(12345)

const baseUrl = 'https://mystore.com'
const pages = [
  '/',
  '/products',
  '/products/electronics',
  '/products/clothing',
  '/cart',
  '/checkout',
  '/account',
  '/about',
  '/contact',
]

export function getRandomPageUrl(): string {
  const page = faker.helpers.arrayElement(pages)
  return `${baseUrl}${page}`
}

export function generateEventData(eventType: EventType, user: User): EventData {
  const user_id = user.id
  const timestamp = Date.now()
  const metadata: Record<string, unknown> = {
    session_id: user.sessionId,
  }

  switch (eventType) {
    case 'click':
      metadata.element_type = faker.helpers.arrayElement([
        'button',
        'link',
        'image',
        'form',
      ])
      metadata.element_id = faker.string.alphanumeric(10)
      metadata.page_url = getRandomPageUrl()
      metadata.coordinates = {
        x: faker.number.int({min: 0, max: 1920}),
        y: faker.number.int({min: 0, max: 1080}),
      }
      break

    case 'page_view':
      metadata.page_url = getRandomPageUrl()
      metadata.page_title = faker.lorem.words(3)
      metadata.referrer = getRandomPageUrl()
      metadata.user_agent = faker.internet.userAgent()
      metadata.viewport = {
        width: faker.number.int({min: 800, max: 1920}),
        height: faker.number.int({min: 600, max: 1080}),
      }
      break

    case 'purchase':
      metadata.product_id = faker.string.alphanumeric(8)
      metadata.product_name = faker.commerce.productName()
      metadata.category = faker.commerce.department()
      metadata.price = parseFloat(faker.commerce.price({min: 10, max: 1000}))
      metadata.currency = faker.finance.currencyCode()
      metadata.payment_method = faker.helpers.arrayElement([
        'credit_card',
        'paypal',
        'bank_transfer',
        'crypto',
      ])
      metadata.order_id = faker.string.uuid()
      metadata.quantity = faker.number.int({min: 1, max: 5})
      break

    case 'add_to_cart':
      metadata.product_id = faker.string.alphanumeric(8)
      metadata.product_name = faker.commerce.productName()
      metadata.category = faker.commerce.department()
      metadata.price = parseFloat(faker.commerce.price({min: 5, max: 500}))
      metadata.currency = faker.finance.currencyCode()
      metadata.quantity = faker.number.int({min: 1, max: 10})
      metadata.cart_total = parseFloat(
        faker.commerce.price({min: 50, max: 2000})
      )
      break

    case 'checkout':
      metadata.cart_total = parseFloat(
        faker.commerce.price({min: 20, max: 1500})
      )
      metadata.currency = faker.finance.currencyCode()
      metadata.items_count = faker.number.int({min: 1, max: 20})
      metadata.payment_method = faker.helpers.arrayElement([
        'credit_card',
        'paypal',
        'bank_transfer',
        'crypto',
      ])
      metadata.shipping_method = faker.helpers.arrayElement([
        'standard',
        'express',
        'overnight',
        'pickup',
      ])
      metadata.discount_applied = faker.datatype.boolean()
      metadata.discount_amount = metadata.discount_applied
        ? parseFloat(faker.commerce.price({min: 5, max: 100}))
        : 0
      break

    case 'favorite':
      metadata.item_type = faker.helpers.arrayElement([
        'product',
        'article',
        'video',
        'playlist',
      ])
      metadata.item_id = faker.string.alphanumeric(8)
      metadata.item_name = faker.commerce.productName()
      metadata.category = faker.commerce.department()
      metadata.is_favorited = faker.datatype.boolean()
      break

    case 'add_review':
      metadata.product_id = faker.string.alphanumeric(8)
      metadata.product_name = faker.commerce.productName()
      metadata.rating = faker.number.int({min: 1, max: 5})
      metadata.review_text = faker.lorem.paragraph()
      metadata.review_title = faker.lorem.sentence()
      metadata.verified_purchase = faker.datatype.boolean()
      metadata.helpful_votes = faker.number.int({min: 0, max: 50})
      metadata.review_id = faker.string.uuid()
      break

    default:
      throw new Error(`Unknown event type: ${eventType}`)
  }

  return {event_type: eventType, user_id, timestamp, metadata}
}
