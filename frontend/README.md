# Vehicle Search - Frontend Application

Next.js 16+ application for the Vehicle Search intelligent search agent.

## Tech Stack

- **Framework:** Next.js 16.1.5 with App Router
- **Language:** TypeScript 5+
- **Styling:** Tailwind CSS 3
- **UI Components:** shadcn/ui (Radix UI primitives)
- **API Client:** Generated from OpenAPI spec
- **Data Fetching:** SWR
- **Testing:** Jest + React Testing Library

## Getting Started

### Prerequisites

- Node.js 20+
- npm 10+
- Backend API running on http://localhost:5001

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Building

```bash
npm run build
```

### Testing

```bash
# Run tests once
npm test

# Run tests in watch mode
npm run test:watch
```

### Linting

```bash
npm run lint
```

## Project Structure

```
frontend/
├── src/
│   ├── app/                    # Next.js App Router
│   │   ├── (routes)/
│   │   │   └── search/         # Search page route
│   │   ├── layout.tsx          # Root layout
│   │   ├── page.tsx            # Home page
│   │   ├── error.tsx           # Error boundary
│   │   ├── loading.tsx         # Loading state
│   │   └── globals.css         # Global styles
│   ├── components/             # React components
│   │   ├── ui/                 # shadcn/ui components
│   │   ├── search/             # Search-specific components (future)
│   │   └── layout/             # Layout components (future)
│   ├── lib/                    # Utilities
│   │   ├── api/                # API client
│   │   │   ├── generated/      # Generated from OpenAPI spec
│   │   │   └── client.ts       # API client wrapper
│   │   ├── hooks/              # Custom React hooks (future)
│   │   └── utils.ts            # Helper functions
│   └── types/                  # TypeScript types
│       └── api.ts              # API types
├── tests/                      # Test files
│   └── pages/                  # Page tests
├── public/                     # Static assets
├── next.config.ts              # Next.js configuration
├── tailwind.config.ts          # Tailwind CSS configuration
├── tsconfig.json               # TypeScript configuration
└── package.json                # Dependencies and scripts
```

## Environment Variables

Create a `.env.local` file:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5001
```

## API Client Generation

The API client is automatically generated from the backend's OpenAPI specification. To regenerate:

1. Ensure the backend is running
2. Fetch the OpenAPI spec: `curl http://localhost:5001/swagger/v1/swagger.json -o openapi.json`
3. Generate the client: `npx @hey-api/openapi-ts -i openapi.json -o src/lib/api/generated`

## Available Components

The following shadcn/ui components are installed:

- Button
- Input
- Card
- Badge
- Skeleton

More components will be added as needed for feature implementation.

## Development Guidelines

- Use server components by default (Next.js 16+)
- Add `'use client'` directive only when necessary
- Follow TypeScript strict mode
- Use path aliases (`@/*` for `src/*`)
- Implement proper error boundaries
- Use semantic HTML elements
- Ensure accessibility (ARIA labels, keyboard navigation)

## Testing Strategy

- Unit tests for components using React Testing Library
- Integration tests for API client
- E2E tests using Playwright (future)
- Target: ≥80% code coverage

## Performance Targets

- Initial page load: <2 seconds
- Client-side navigation: instant
- API response handling: <3 seconds
- Lighthouse score: >90 (accessibility, best practices)

## Accessibility

- WCAG 2.1 Level AA compliance
- Semantic HTML
- ARIA labels where needed
- Keyboard navigation support
- Screen reader compatibility
- Sufficient color contrast

## License

MIT

