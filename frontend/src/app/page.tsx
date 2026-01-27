import Link from 'next/link'

export default function Home() {
  return (
    <main className="container mx-auto px-4 py-16">
      <h1 className="text-4xl font-bold mb-4">
        Vehicle Search Agent
      </h1>
      <p className="text-lg text-muted-foreground mb-8">
        Search for vehicles using natural language
      </p>
      <Link 
        href="/search"
        className="inline-block px-6 py-3 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90"
      >
        Start Searching
      </Link>
    </main>
  )
}
