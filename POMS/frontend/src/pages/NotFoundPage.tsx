import { Link } from 'react-router-dom'
import { Compass } from 'lucide-react'
import { EmptyState } from '../components/Ui'

export function NotFoundPage() {
  return <div className="card"><EmptyState icon={<Compass />} title="Page not found" description="The page you opened does not exist or has moved." action={<Link className="btn btn-primary" to="/">Go to dashboard</Link>} /></div>
}
