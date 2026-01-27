# Task: Frontend Application Scaffolding

**Task ID:** 002  
**Feature:** Infrastructure Foundation  
**Type:** Scaffolding  
**Priority:** Critical (Must complete before frontend features)  
**Estimated Complexity:** Medium

---

## Description

Create the foundational Next.js 15 application with TypeScript, Tailwind CSS, component architecture, and API client generation from OpenAPI spec that all frontend feature tasks will build upon.

---

## Dependencies

**Depends on:**
- Task 001: Backend API Scaffolding (for OpenAPI spec generation)

**Blocks:**
- All frontend feature tasks
- Frontend UI component tasks
- E2E testing tasks

---

## Technical Requirements

### Project Setup

**Framework:** Next.js 15 with App Router
**Language:** TypeScript 5+
**Styling:** Tailwind CSS + shadcn/ui
**Package Manager:** npm or pnpm

### Project Structure

```
frontend/
├── src/
│   ├── app/                    # Next.js App Router
│   │   ├── (routes)/
│   │   │   └── search/
│   │   │       └── page.tsx
│   │   ├── layout.tsx          # Root layout
│   │   ├── page.tsx            # Home page
│   │   └── globals.css         # Global styles
│   ├── components/             # React components
│   │   ├── ui/                 # shadcn/ui components
│   │   ├── search/             # Search-specific components
│   │   └── layout/             # Layout components
│   ├── lib/                    # Utilities
│   │   ├── api/                # Generated API client
│   │   ├── hooks/              # Custom React hooks
│   │   └── utils.ts            # Helper functions
│   └── types/                  # TypeScript types
│       └── api.ts              # API types
├── public/                     # Static assets
├── tests/                      # Test files
├── next.config.js              # Next.js config
├── tailwind.config.ts          # Tailwind config
├── tsconfig.json               # TypeScript config
└── package.json
```

### Required Dependencies

**Core:**
```json
{
  "dependencies": {
    "next": "^15.0.0",
    "react": "^18.3.0",
    "react-dom": "^18.3.0",
    "typescript": "^5.0.0"
  }
}
```

**Styling:**
- `tailwindcss`
- `@tailwindcss/typography`
- `tailwind-merge`
- `clsx`

**UI Components:**
- `@radix-ui/react-*` (various Radix primitives)
- `lucide-react` (icons)
- `class-variance-authority` (component variants)

**API Client:**
- `openapi-typescript-codegen` or `@hey-api/openapi-ts` (code generation)
- `axios` or `fetch` (HTTP client)

**State & Data Fetching:**
- `swr` or `@tanstack/react-query` (data fetching)
- React Context API (conversation state)

**Development:**
- `@types/node`
- `@types/react`
- `@types/react-dom`
- `eslint`
- `eslint-config-next`

### Next.js Configuration

**next.config.js:**
```javascript
/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'
  },
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5001/api/:path*' // Proxy to backend
      }
    ]
  }
}

module.exports = nextConfig
```

### TypeScript Configuration

**tsconfig.json:**
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["dom", "dom.iterable", "esnext"],
    "allowJs": true,
    "skipLibCheck": true,
    "strict": true,
    "forceConsistentCasingInFileNames": true,
    "noEmit": true,
    "esModuleInterop": true,
    "module": "esnext",
    "moduleResolution": "bundler",
    "resolveJsonModule": true,
    "isolatedModules": true,
    "jsx": "preserve",
    "incremental": true,
    "plugins": [{ "name": "next" }],
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx", ".next/types/**/*.ts"],
  "exclude": ["node_modules"]
}
```

### Tailwind CSS Setup

**tailwind.config.ts:**
```typescript
import type { Config } from 'tailwindcss'

const config: Config = {
  content: [
    './src/**/*.{js,ts,jsx,tsx,mdx}'
  ],
  theme: {
    extend: {
      colors: {
        // Custom colors for vehicle search theme
      }
    }
  },
  plugins: [require('@tailwindcss/typography')]
}

export default config
```

### shadcn/ui Integration

**Initialize shadcn/ui:**
- Run `npx shadcn-ui@latest init`
- Configure with Tailwind CSS
- Set up component folder structure
- Install essential components:
  - `button`
  - `input`
  - `card`
  - `dialog`
  - `badge`
  - `skeleton` (loading states)

### API Client Generation

**Process:**
1. Generate TypeScript client from OpenAPI spec (from backend)
2. Create API client wrapper with error handling
3. Configure base URL from environment
4. Include authentication headers (placeholder for v2)

**API Client Structure:**
```typescript
// lib/api/client.ts
import { DefaultApi } from './generated' // Generated from OpenAPI

export const apiClient = new DefaultApi({
  basePath: process.env.NEXT_PUBLIC_API_URL,
  // Add interceptors for error handling
})

// lib/api/search.ts
export async function searchVehicles(query: string) {
  try {
    const response = await apiClient.searchVehicles({ query })
    return response.data
  } catch (error) {
    // Handle errors
    throw error
  }
}
```

### Root Layout

**app/layout.tsx:**
```typescript
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
```

### Home Page (Placeholder)

**app/page.tsx:**
```typescript
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
        className="inline-block px-6 py-3 bg-primary text-primary-foreground rounded-lg"
      >
        Start Searching
      </Link>
    </main>
  )
}
```

### Environment Configuration

**.env.local:**
```bash
NEXT_PUBLIC_API_URL=http://localhost:5001
```

**.env.production:**
```bash
NEXT_PUBLIC_API_URL=https://api.production.com
```

### Error Handling

**Global Error Boundary:**
- Create error.tsx in app directory
- Handle and display errors gracefully
- Log errors (console in dev, service in prod)

### Loading States

**Global Loading:**
- Create loading.tsx in app directory
- Use Suspense boundaries appropriately
- Skeleton components for async data

---

## Acceptance Criteria

### Functional Criteria

✅ **Project Setup:**
- [ ] Next.js 15 app created and runs successfully
- [ ] TypeScript configured without errors
- [ ] Tailwind CSS working (styles apply)
- [ ] App accessible at `http://localhost:3000`

✅ **Structure:**
- [ ] All folders created per specification
- [ ] shadcn/ui initialized and working
- [ ] Essential UI components installed
- [ ] API client folder structure ready

✅ **API Integration:**
- [ ] OpenAPI spec successfully fetched from backend
- [ ] TypeScript client generated from spec
- [ ] API client configured with correct base URL
- [ ] Test API call to health endpoint succeeds

✅ **Navigation:**
- [ ] Home page renders correctly
- [ ] Link to search page works
- [ ] Routing functional (client-side navigation)

✅ **Styling:**
- [ ] Tailwind classes apply correctly
- [ ] Theme colors configured
- [ ] Responsive breakpoints work
- [ ] shadcn/ui components styled correctly

### Technical Criteria

✅ **Code Quality:**
- [ ] No TypeScript errors
- [ ] No ESLint warnings
- [ ] Follows naming conventions from AGENTS.md
- [ ] All components properly typed

✅ **Configuration:**
- [ ] Environment variables load correctly
- [ ] API proxy configuration works
- [ ] Build process completes without errors
- [ ] Development server starts quickly (<5s)

✅ **Performance:**
- [ ] Initial page load <2 seconds
- [ ] Client-side navigation instant
- [ ] No console errors or warnings
- [ ] Lighthouse score >90 (accessibility, best practices)

---

## Testing Requirements

### Component Tests

**Test Coverage:** ≥80% (once components added)

**Initial Tests:**
- [ ] Home page renders
- [ ] Navigation links work
- [ ] Error boundary catches errors
- [ ] Loading states display

**Testing Framework:**
- React Testing Library
- Jest or Vitest
- Testing playground for query selectors

**Test Structure:**
```
tests/
├── components/
│   └── ui/
├── pages/
│   └── home.test.tsx
└── lib/
    └── api.test.ts
```

### E2E Tests (Future)

**Framework:** Playwright or Cypress
**Coverage:** Critical user flows

**Initial E2E Tests:**
- [ ] User can navigate to search page
- [ ] API client connects to backend

---

## Implementation Notes

### DO:
- ✅ Use server components by default (Next.js 15)
- ✅ Add 'use client' only when necessary
- ✅ Use TypeScript strict mode
- ✅ Configure path aliases (@/*)
- ✅ Setup proper error boundaries
- ✅ Use semantic HTML elements
- ✅ Make components accessible (ARIA labels)

### DON'T:
- ❌ Include actual search UI yet (that's in feature tasks)
- ❌ Implement conversation logic yet
- ❌ Add authentication (v2)
- ❌ Use any external component library besides shadcn/ui
- ❌ Skip accessibility considerations

### Best Practices:
- Server components for static content
- Client components for interactivity
- Use Suspense for async operations
- Optimize images with next/image
- Lazy load heavy components

---

## Definition of Done

- [ ] Next.js app runs without errors
- [ ] Home page accessible and styled
- [ ] Search page route exists (placeholder)
- [ ] API client generated and configured
- [ ] Test API call to backend succeeds
- [ ] Tailwind CSS working
- [ ] shadcn/ui components installed
- [ ] All TypeScript errors resolved
- [ ] ESLint configured and passing
- [ ] Component tests pass
- [ ] Build succeeds (npm run build)
- [ ] Code reviewed and approved
- [ ] Documentation updated

---

## Related Files

**Created/Modified:**
- `frontend/package.json`
- `frontend/next.config.js`
- `frontend/tailwind.config.ts`
- `frontend/tsconfig.json`
- `frontend/src/app/layout.tsx`
- `frontend/src/app/page.tsx`
- `frontend/src/lib/api/**`
- `frontend/src/components/ui/**`

**References:**
- AGENTS.md (Frontend Standards)
- Task 001 (Backend API for OpenAPI spec)
- PRD Section 8 (UX/Accessibility requirements)
