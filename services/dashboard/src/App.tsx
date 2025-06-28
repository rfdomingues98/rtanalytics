import {useEffect, useState} from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import './App.css'

interface AnalyticsEvent {
  id: string
  timestamp: number
  userId: string
  eventType: 'page_view' | 'click' | 'purchase' | 'signup'
  data: Record<string, any>
}

interface EventCount {
  eventType: string
  count: number
}

interface TimeSeriesData {
  time: string
  events: number
}

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042']

function App() {
  const [events, setEvents] = useState<AnalyticsEvent[]>([])
  const [isConnected, setIsConnected] = useState(false)
  const [eventCounts, setEventCounts] = useState<EventCount[]>([])
  const [timeSeriesData, setTimeSeriesData] = useState<TimeSeriesData[]>([])

  useEffect(() => {
    const ws = new WebSocket('ws://localhost:8080')

    ws.onopen = () => {
      console.log('Connected to Event Generator')
      setIsConnected(true)
    }

    ws.onmessage = (event) => {
      try {
        const analyticsEvent: AnalyticsEvent = JSON.parse(event.data)
        setEvents((prev) => [analyticsEvent, ...prev.slice(0, 99)]) // Keep last 100 events
      } catch (error) {
        console.error('Error parsing event:', error)
      }
    }

    ws.onclose = () => {
      console.log('Disconnected from Event Generator')
      setIsConnected(false)
    }

    ws.onerror = (error) => {
      console.error('WebSocket error:', error)
      setIsConnected(false)
    }

    return () => {
      ws.close()
    }
  }, [])

  // Update event counts when events change
  useEffect(() => {
    const counts = events.reduce((acc, event) => {
      const existingCount = acc.find((c) => c.eventType === event.eventType)
      if (existingCount) {
        existingCount.count++
      } else {
        acc.push({eventType: event.eventType, count: 1})
      }
      return acc
    }, [] as EventCount[])

    setEventCounts(counts)
  }, [events])

  // Update time series data
  useEffect(() => {
    const now = Date.now()
    const timeSlots = Array.from({length: 10}, (_, i) => {
      const time = now - (9 - i) * 60000 // 10 minutes, 1-minute intervals
      const timeStr = new Date(time).toLocaleTimeString()
      const eventsInSlot = events.filter(
        (e) => e.timestamp >= time - 30000 && e.timestamp < time + 30000
      ).length

      return {time: timeStr, events: eventsInSlot}
    })

    setTimeSeriesData(timeSlots)
  }, [events])

  const formatTimestamp = (timestamp: number) => {
    return new Date(timestamp).toLocaleTimeString()
  }

  return (
    <div className='app'>
      <header className='app-header'>
        <h1>Real-Time Analytics Dashboard</h1>
        <div
          className={`connection-status ${
            isConnected ? 'connected' : 'disconnected'
          }`}
        >
          {isConnected ? '🟢 Connected' : '🔴 Disconnected'}
        </div>
      </header>

      <div className='dashboard-grid'>
        <div className='card'>
          <h2>Events Over Time</h2>
          <ResponsiveContainer width='100%' height={300}>
            <LineChart data={timeSeriesData}>
              <CartesianGrid strokeDasharray='3 3' />
              <XAxis dataKey='time' />
              <YAxis />
              <Tooltip />
              <Line
                type='monotone'
                dataKey='events'
                stroke='#8884d8'
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className='card'>
          <h2>Event Types Distribution</h2>
          <ResponsiveContainer width='100%' height={300}>
            <PieChart>
              <Pie
                data={eventCounts}
                cx='50%'
                cy='50%'
                labelLine={false}
                label={({eventType, count}) => `${eventType}: ${count}`}
                outerRadius={80}
                fill='#8884d8'
                dataKey='count'
              >
                {eventCounts.map((entry, index) => (
                  <Cell
                    key={`cell-${index}`}
                    fill={COLORS[index % COLORS.length]}
                  />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className='card'>
          <h2>Event Counts by Type</h2>
          <ResponsiveContainer width='100%' height={300}>
            <BarChart data={eventCounts}>
              <CartesianGrid strokeDasharray='3 3' />
              <XAxis dataKey='eventType' />
              <YAxis />
              <Tooltip />
              <Bar dataKey='count' fill='#8884d8' />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className='card'>
          <h2>Recent Events</h2>
          <div className='events-list'>
            {events.slice(0, 10).map((event) => (
              <div key={event.id} className='event-item'>
                <div className='event-header'>
                  <span className={`event-type ${event.eventType}`}>
                    {event.eventType}
                  </span>
                  <span className='event-time'>
                    {formatTimestamp(event.timestamp)}
                  </span>
                </div>
                <div className='event-details'>
                  <span className='user-id'>{event.userId}</span>
                  <span className='event-data'>
                    {JSON.stringify(event.data)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <footer className='stats'>
        <div className='stat'>
          <span className='stat-value'>{events.length}</span>
          <span className='stat-label'>Total Events</span>
        </div>
        <div className='stat'>
          <span className='stat-value'>
            {new Set(events.map((e) => e.userId)).size}
          </span>
          <span className='stat-label'>Unique Users</span>
        </div>
        <div className='stat'>
          <span className='stat-value'>{eventCounts.length}</span>
          <span className='stat-label'>Event Types</span>
        </div>
      </footer>
    </div>
  )
}

export default App
