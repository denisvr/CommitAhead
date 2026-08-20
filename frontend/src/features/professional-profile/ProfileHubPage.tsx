import { CVPresentationDetailPage } from '../cv-presentations/CVPresentationDetailPage'
import { CVPresentationForm } from '../cv-presentations/CVPresentationForm'
import { CVPresentationsListPage } from '../cv-presentations/CVPresentationsListPage'
import { HomePage } from '../home/HomePage'
import { ProfessionalProfilePage } from './ProfessionalProfilePage'

export type HubTab = 'home' | 'profile' | 'presentations'

export type PresentationsView = { name: 'list' } | { name: 'detail'; id: string } | { name: 'new' }

type ProfileHubPageProps = {
  hubTab: HubTab
  onHubTabChange: (tab: HubTab) => void
  presentationsView: PresentationsView
  onPresentationsViewChange: (view: PresentationsView) => void
  onCreateCV: () => void
}

// Content only — navigation between tabs happens in AppShell's Sidebar (Home/Profile/CV
// presentations) and via the brand mark (back to Home). App.tsx is the common ancestor that owns
// hubTab/presentationsView so those controls actually decide what shows here. Page-level actions
// (Import from LinkedIn, Create a CV) now live on the feature pages themselves, not a shared
// header slot — each page owns the actions that belong to it.
export function ProfileHubPage({ hubTab, onHubTabChange, presentationsView, onPresentationsViewChange, onCreateCV }: ProfileHubPageProps) {
  return (
    <>
      {hubTab === 'home' && (
        <HomePage onOpenProfile={() => onHubTabChange('profile')} onOpenPresentations={() => onHubTabChange('presentations')} onCreateCV={onCreateCV} />
      )}

      {hubTab === 'profile' && <ProfessionalProfilePage />}

      {hubTab === 'presentations' && presentationsView.name === 'list' && (
        <CVPresentationsListPage
          onSelectPresentation={(id) => onPresentationsViewChange({ name: 'detail', id })}
          onCreateNew={() => onPresentationsViewChange({ name: 'new' })}
        />
      )}

      {hubTab === 'presentations' && presentationsView.name === 'detail' && (
        <CVPresentationDetailPage
          key={presentationsView.id}
          presentationId={presentationsView.id}
          onBack={() => onPresentationsViewChange({ name: 'list' })}
          onDeleted={() => onPresentationsViewChange({ name: 'list' })}
        />
      )}

      {hubTab === 'presentations' && presentationsView.name === 'new' && (
        <CVPresentationForm
          mode="create"
          onCreated={(id) => onPresentationsViewChange({ name: 'detail', id })}
          onCancel={() => onPresentationsViewChange({ name: 'list' })}
          onGoToProfile={() => onHubTabChange('profile')}
        />
      )}
    </>
  )
}
