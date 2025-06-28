# Real-Time Analytics Microservices

A microservices architecture for real-time analytics built with modern technologies.

## Architecture

- **Event Generator** (Node.js + TypeScript) - Generates real-time events
- **Event Processor** (.NET) - Processes events in real-time
- **Dashboard** (React + TypeScript + Vite) - Visualizes analytics data

## Prerequisites

- Node.js (v18 or later)
- .NET 8 SDK
- pnpm

## Setup

1. Install dependencies:
   ```bash
   pnpm install:all
   ```

2. Start all services:
   ```bash
   # Start event generator and dashboard concurrently
   pnpm dev
   
   # In another terminal, start the event processor
   pnpm start:event-processor
   ```

## Individual Service Commands

- **Event Generator**: `pnpm start:event-generator`
- **Event Processor**: `pnpm start:event-processor`  
- **Dashboard**: `pnpm start:dashboard`

## Project Structure

```
real-time-analytics/
├── services/
│   ├── event-generator/     # Node.js TypeScript service
│   ├── event-processor/     # .NET service
│   └── dashboard/           # React TypeScript frontend
├── package.json
├── pnpm-workspace.yaml
└── README.md
``` 