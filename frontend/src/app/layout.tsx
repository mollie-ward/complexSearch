import type { Metadata } from 'next'
import './globals.css'

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
          {children}
        </div>
      </body>
    </html>
  )
}
