import { render, screen } from '@testing-library/react'
import Home from '@/app/page'

describe('Home Page', () => {
  it('renders the main heading', () => {
    render(<Home />)
    const heading = screen.getByRole('heading', { name: /Vehicle Search Agent/i })
    expect(heading).toBeInTheDocument()
  })

  it('renders the description text', () => {
    render(<Home />)
    const description = screen.getByText(/Search for vehicles using natural language/i)
    expect(description).toBeInTheDocument()
  })

  it('renders the start searching link', () => {
    render(<Home />)
    const link = screen.getByRole('link', { name: /Start Searching/i })
    expect(link).toBeInTheDocument()
    expect(link).toHaveAttribute('href', '/search')
  })
})
