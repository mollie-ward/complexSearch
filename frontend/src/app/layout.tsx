import type { Metadata } from 'next'
import './globals.css'
import { ComparisonProvider } from '@/lib/context/ComparisonContext'

export const metadata: Metadata = {
  title: 'Vehicle Search - Intelligent Search Agent',
  description: 'Search vehicles using natural language'
}

export default function RootLayout({
  children
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body>
        <div className="min-h-screen bg-background">
          <ComparisonProvider>
            {children}
          </ComparisonProvider>
        </div>
      </body>
    </html>
  )
}
